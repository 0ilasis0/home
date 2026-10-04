import shutil
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from .constants import ICC_HEADER_SIZE
from .errors import InvalidVcgtError, ProfileError, ProfileWriteError
from .models import VcgtTable
from .parser import parse_profile_tags


@dataclass
class DataElement:
    """
    Internal structure to decouple logical tags from physical binary data blocks.
    Used to safely preserve shared tag payloads and 4-byte alignments.
    """
    payload: bytes
    original_offset: int = -1
    new_offset: int = 0


def serialize_vcgt(table: VcgtTable) -> bytes:
    """
    Serializes a VcgtTable into a valid ICC vcgt Table Type binary payload (Big-Endian).
    """
    if len(table.red) != 256 or len(table.green) != 256 or len(table.blue) != 256:
        raise InvalidVcgtError("VcgtTable channels must have exactly 256 entries.")

    for channel_name, channel_data in [('red', table.red), ('green', table.green), ('blue', table.blue)]:
        for val in channel_data:
            if not (0 <= val <= 0xFFFF):
                raise InvalidVcgtError(f"Value out of uint16 range in {channel_name} channel: {val}")

    # Header: 4s(sig) 4s(reserved) I(gammaType) H(channels) H(entry_count) H(entry_size)
    header = struct.pack(">4s 4s I H H H", b"vcgt", b"\x00\x00\x00\x00", 0, 3, 256, 2)

    fmt = ">256H"
    red_bytes = struct.pack(fmt, *table.red)
    green_bytes = struct.pack(fmt, *table.green)
    blue_bytes = struct.pack(fmt, *table.blue)

    return header + red_bytes + green_bytes + blue_bytes


def _rebuild_profile_safely(
    input_path: Path,
    output_path: Path,
    new_vcgt_bytes: Optional[bytes],
    tags_to_delete: set[str]
) -> None:
    """
    Core engine for ICC profile rebuilding.
    - Decouples Tag Table Entries from physical Data Elements.
    - Accurately preserves original shared Data Element relationships.
    - Enforces 4-byte alignments for all output payload offsets.
    """
    try:
        tags = parse_profile_tags(input_path)
    except ProfileError as e:
        raise ProfileWriteError(f"Failed to parse input profile: {e}") from e

    original_elements: dict[tuple[int, int], DataElement] = {}

    try:
        with input_path.open("rb") as f:
            header_data = f.read(ICC_HEADER_SIZE)
            if len(header_data) < ICC_HEADER_SIZE:
                raise ProfileWriteError("Input profile header is too short.")

            for tag in tags:
                key = (tag.offset, tag.size)
                if key not in original_elements:
                    f.seek(tag.offset)
                    data = f.read(tag.size)

                    # [FIX] 確保實際讀取到的位元組長度等於標籤宣告的長度
                    if len(data) != tag.size:
                        raise ProfileWriteError(
                            f"File truncated while reading tag payload at offset {tag.offset} "
                            f"(expected {tag.size}, got {len(data)})."
                        )

                    original_elements[key] = DataElement(payload=data, original_offset=tag.offset)
    except OSError as e:
        raise ProfileWriteError(f"Failed to read input profile payloads: {e}") from e

    logical_tags: list[tuple[bytes, DataElement]] = []
    vcgt_seen = False
    new_vcgt_element = DataElement(payload=new_vcgt_bytes) if new_vcgt_bytes else None

    # 分配邏輯標籤
    for tag in tags:
        if tag.signature == "vcgt":
            if vcgt_seen:
                raise ProfileWriteError("Input profile contains duplicate 'vcgt' tags.")
            vcgt_seen = True

            if new_vcgt_bytes is not None:
                logical_tags.append((b"vcgt", new_vcgt_element))
            elif "vcgt" not in tags_to_delete:
                key = (tag.offset, tag.size)
                logical_tags.append((b"vcgt", original_elements[key]))
        else:
            if tag.signature not in tags_to_delete:
                key = (tag.offset, tag.size)
                sig_bytes = tag.signature.encode("ascii")
                logical_tags.append((sig_bytes, original_elements[key]))

    # 若原本沒有 vcgt 但需要寫入，附加在最後
    if new_vcgt_bytes is not None and not vcgt_seen:
        logical_tags.append((b"vcgt", new_vcgt_element))

    # 過濾出需要實際寫入檔案的實體資料區塊 (根據記憶體位置 id 來過濾，保留共享特性)
    unique_elements = []
    seen_ids = set()
    for _, elem in logical_tags:
        if id(elem) not in seen_ids:
            seen_ids.add(id(elem))
            unique_elements.append(elem)

    # 確保原本的實體資料順序盡量不變，新附加的資料排在最後
    unique_elements.sort(key=lambda e: e.original_offset if e.original_offset != -1 else float('inf'))

    tag_count = len(logical_tags)
    # 計算起始 Offset：Header(128) + Tag Count(4) + (Count * 12)
    current_offset = ICC_HEADER_SIZE + 4 + (tag_count * 12)
    data_bytes = bytearray()

    # 計算並寫入各個實體資料塊，強制對齊 4-byte 邊界
    for elem in unique_elements:
        align_pad = (4 - (current_offset % 4)) % 4
        if align_pad > 0:
            data_bytes += b"\x00" * align_pad
            current_offset += align_pad

        elem.new_offset = current_offset
        data_bytes += elem.payload
        current_offset += len(elem.payload)

    # 建立 Tag Table 指標 (將邏輯標籤對應回更新後的實體位址)
    tag_table_bytes = bytearray()
    for sig_bytes, elem in logical_tags:
        tag_table_bytes += struct.pack(">4sII", sig_bytes, elem.new_offset, len(elem.payload))

    # 重算 Profile 總長度並更新 Header
    total_size = ICC_HEADER_SIZE + 4 + len(tag_table_bytes) + len(data_bytes)
    new_header = bytearray(header_data)
    new_header[0:4] = struct.pack(">I", total_size)

    # 組裝最終的 ICC Binary
    final_binary = new_header + struct.pack(">I", tag_count) + tag_table_bytes + data_bytes

    try:
        with output_path.open("wb") as f:
            f.write(final_binary)
    except OSError as e:
        raise ProfileWriteError(f"Failed to write output profile: {e}") from e


def save_profile(
    input_path: Path,
    output_path: Path,
    vcgt_table: Optional[VcgtTable] = None,
    tags_to_delete: Optional[set[str]] = None
) -> None:
    if tags_to_delete is None:
        tags_to_delete = set()

    # [FIX] 恢復 TASK-005 要求：嚴格驗證 tags_to_delete 的 signature 合法性
    for sig in tags_to_delete:
        if not isinstance(sig, str) or len(sig) != 4 or not sig.isascii():
            raise ProfileWriteError(f"Invalid signature '{sig}': must be exactly 4 ASCII characters.")

    if not input_path.exists():
        raise ProfileWriteError(f"Input file does not exist: {input_path}")
    if input_path.resolve() == output_path.resolve():
        raise ProfileWriteError("Input and output paths must not be the same to prevent data corruption.")

    # No-op save optimization: Byte-for-byte 完美複製
    if vcgt_table is None and not tags_to_delete:
        try:
            shutil.copy2(input_path, output_path)
        except OSError as e:
            raise ProfileWriteError(f"Failed to copy profile: {e}") from e
        return

    try:
        new_vcgt_bytes = serialize_vcgt(vcgt_table) if vcgt_table else None
    except InvalidVcgtError as e:
        raise ProfileWriteError(f"Failed to serialize VcgtTable: {e}") from e

    # 委派至新的安全重建引擎
    _rebuild_profile_safely(input_path, output_path, new_vcgt_bytes, tags_to_delete)


# ==========================================
# Legacy API Bridges (For backward compatibility)
# ==========================================

def add_or_replace_vcgt(input_path: Path, output_path: Path, table: VcgtTable) -> None:
    """
    Creates a new ICC Profile by adding or replacing the vcgt tag.
    (Legacy API: Routes to save_profile)
    """
    save_profile(input_path, output_path, vcgt_table=table)


def delete_tags(input_path: Path, output_path: Path, signatures: set[str]) -> None:
    """
    Removes the specified tags from the ICC profile and writes the result to a new file.
    (Legacy API: Routes to save_profile)
    """
    save_profile(input_path, output_path, tags_to_delete=signatures)