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

def _parse_desc_string(payload: bytes) -> Optional[str]:
    """Helper to safely extract the string from a 'desc' or 'mluc' tag payload."""
    if len(payload) < 8:
        return None
    sig = payload[:4]
    if sig == b"desc":
        if len(payload) >= 12:
            ascii_len = struct.unpack(">I", payload[8:12])[0]
            if len(payload) >= 12 + ascii_len:
                ascii_bytes = payload[12:12+ascii_len]
                return ascii_bytes.strip(b'\x00').decode('ascii', errors='ignore')
    elif sig == b"mluc":
        if len(payload) >= 16:
            num_records, record_size = struct.unpack(">II", payload[8:16])
            # [FIX] 嚴格驗證 record_size，避免 Malformed Payload 導致後續 unpack 錯誤
            if record_size < 12:
                raise ProfileWriteError(f"Malformed mluc payload: invalid record_size {record_size}")

            if num_records > 0 and len(payload) >= 16 + record_size:
                lang, country, str_len, str_offset = struct.unpack(">2s2sII", payload[16:16+12])
                if len(payload) >= str_offset + str_len:
                    uni_bytes = payload[str_offset:str_offset+str_len]
                    return uni_bytes.decode('utf-16be', errors='ignore').strip('\x00')
    return None

def _update_desc_payload(original_payload: bytes, new_name: str) -> bytes:
    """Generates a new desc payload preserving the original structure type (desc or mluc)."""
    if len(original_payload) < 8:
        raise ProfileWriteError("Original desc payload is too short.")
    sig = original_payload[:4]

    try:
        if sig == b"desc":
            ascii_bytes = new_name.encode("ascii") + b"\x00"
            uni_bytes = new_name.encode("utf-16be")
            uc_chars = len(uni_bytes) // 2

            return struct.pack(
                f">4s I I {len(ascii_bytes)}s I I {len(uni_bytes)}s H B 67s",
                b"desc", 0, len(ascii_bytes), ascii_bytes,
                0, uc_chars, uni_bytes,
                0, 0, b"\x00" * 67
            )
        elif sig == b"mluc":
            if len(original_payload) < 16:
                raise ProfileWriteError("Malformed mluc payload: too short.")
            num_records, record_size = struct.unpack(">II", original_payload[8:16])

            if record_size < 12:
                raise ProfileWriteError(f"Malformed mluc payload: invalid record_size {record_size}")

            # [FIX] 提取並保留所有現有的 language/country 紀錄
            locales = []
            for i in range(num_records):
                rec_start = 16 + (i * record_size)
                if len(original_payload) < rec_start + 4:
                    raise ProfileWriteError("Malformed mluc payload: truncated records array.")
                lang, country = struct.unpack(">2s2s", original_payload[rec_start:rec_start+4])
                locales.append((lang, country))

            uni_bytes = new_name.encode("utf-16be")
            new_record_size = 12
            str_offset = 16 + (num_records * new_record_size)

            header = struct.pack(">4s I I I", b"mluc", 0, num_records, new_record_size)
            records_bytes = bytearray()

            # 讓所有 locale 都指向同一個新的字串 Offset，這是 ICC 規範中極為標準且高效的做法
            for lang, country in locales:
                records_bytes += struct.pack(">2s2sII", lang, country, len(uni_bytes), str_offset)

            return header + records_bytes + uni_bytes
        else:
            raise ProfileWriteError(f"Unsupported desc tag signature format: {sig.decode('ascii', errors='ignore')}")
    except UnicodeEncodeError as e:
        raise ProfileWriteError(f"Filename contains characters unsupported by ICC desc encoding: {e}")

def _rebuild_profile_safely(
    output_path: Path,
    header_data: bytes,
    tags: list,
    original_elements: dict[tuple[int, int], DataElement],
    new_vcgt_bytes: Optional[bytes],
    new_desc_bytes: Optional[bytes],
    tags_to_delete: set[str]
) -> None:
    logical_tags: list[tuple[bytes, DataElement]] = []
    vcgt_seen = False
    new_vcgt_element = DataElement(payload=new_vcgt_bytes) if new_vcgt_bytes else None
    # [NEW] 獨立的 desc 實體物件，確保與其他標籤解耦
    new_desc_element = DataElement(payload=new_desc_bytes) if new_desc_bytes else None

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

        elif tag.signature == "desc":
            if "desc" not in tags_to_delete:
                if new_desc_bytes is not None:
                    # 使用新的實體物件 (分離舊有的共享關係)
                    logical_tags.append((b"desc", new_desc_element))
                else:
                    key = (tag.offset, tag.size)
                    logical_tags.append((b"desc", original_elements[key]))
        else:
            if tag.signature not in tags_to_delete:
                key = (tag.offset, tag.size)
                sig_bytes = tag.signature.encode("ascii")
                logical_tags.append((sig_bytes, original_elements[key]))

    if new_vcgt_bytes is not None and not vcgt_seen:
        logical_tags.append((b"vcgt", new_vcgt_element))

    unique_elements = []
    seen_ids = set()
    for _, elem in logical_tags:
        if id(elem) not in seen_ids:
            seen_ids.add(id(elem))
            unique_elements.append(elem)

    unique_elements.sort(key=lambda e: e.original_offset if e.original_offset != -1 else float('inf'))

    tag_count = len(logical_tags)
    current_offset = ICC_HEADER_SIZE + 4 + (tag_count * 12)
    data_bytes = bytearray()

    for elem in unique_elements:
        align_pad = (4 - (current_offset % 4)) % 4
        if align_pad > 0:
            data_bytes += b"\x00" * align_pad
            current_offset += align_pad

        elem.new_offset = current_offset
        data_bytes += elem.payload
        current_offset += len(elem.payload)

    tag_table_bytes = bytearray()
    for sig_bytes, elem in logical_tags:
        tag_table_bytes += struct.pack(">4sII", sig_bytes, elem.new_offset, len(elem.payload))

    total_size = ICC_HEADER_SIZE + 4 + len(tag_table_bytes) + len(data_bytes)

    new_header = bytearray(header_data)
    new_header[0:4] = struct.pack(">I", total_size)
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

    for sig in tags_to_delete:
        if not isinstance(sig, str) or len(sig) != 4 or not sig.isascii():
            raise ProfileWriteError(f"Invalid signature '{sig}': must be exactly 4 ASCII characters.")

    if not input_path.exists():
        raise ProfileWriteError(f"Input file does not exist: {input_path}")
    if input_path.resolve() == output_path.resolve():
        raise ProfileWriteError("Input and output paths must not be the same to prevent data corruption.")

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
                    if len(data) != tag.size:
                        raise ProfileWriteError(
                            f"File truncated while reading tag payload at offset {tag.offset} "
                            f"(expected {tag.size}, got {len(data)})."
                        )
                    original_elements[key] = DataElement(payload=data, original_offset=tag.offset)
    except OSError as e:
        raise ProfileWriteError(f"Failed to read input profile payloads: {e}") from e

    # [NEW] Check desc modification
    target_name = output_path.stem
    desc_tag = next((t for t in tags if t.signature == "desc"), None)
    desc_modified = False
    new_desc_bytes = None

    if desc_tag and "desc" not in tags_to_delete:
        orig_desc_payload = original_elements[(desc_tag.offset, desc_tag.size)].payload
        current_desc = _parse_desc_string(orig_desc_payload)
        # 僅當內部字串不符合輸出檔名時，進行 desc 修改
        if current_desc != target_name:
            new_desc_bytes = _update_desc_payload(orig_desc_payload, target_name)
            desc_modified = True

    # [MODIFIED] No-op save optimization updated to respect desc modifications
    if vcgt_table is None and not tags_to_delete and not desc_modified:
        try:
            shutil.copy2(input_path, output_path)
        except OSError as e:
            raise ProfileWriteError(f"Failed to copy profile: {e}") from e
        return

    try:
        new_vcgt_bytes = serialize_vcgt(vcgt_table) if vcgt_table else None
    except InvalidVcgtError as e:
        raise ProfileWriteError(f"Failed to serialize VcgtTable: {e}") from e

    _rebuild_profile_safely(
        output_path, header_data, tags, original_elements,
        new_vcgt_bytes, new_desc_bytes, tags_to_delete
    )


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