import struct
from pathlib import Path

from .constants import ICC_HEADER_SIZE
from .errors import InvalidVcgtError, ProfileError, ProfileWriteError
from .models import VcgtTable
from .parser import parse_profile_tags


def _write_rebuilt_profile(output_path: Path, header_data: bytes, tag_records: list[tuple[bytes, bytes]]) -> None:
    """
    Private helper to calculate offsets, alignment paddings, rebuild the Tag Table,
    update the Profile Size in the header, and write the final ICC binary.
    """
    tag_count = len(tag_records)
    tag_table_bytes = bytearray()
    data_bytes = bytearray()

    current_offset = ICC_HEADER_SIZE + 4 + (tag_count * 12)

    for sig_bytes, payload in tag_records:
        size = len(payload)
        tag_table_bytes += struct.pack(">4sII", sig_bytes, current_offset, size)

        data_bytes += payload

        # ICC 規範: Tag payload 必須 padding 至 4-byte boundary。
        pad_len = (4 - (size % 4)) % 4
        data_bytes += b"\x00" * pad_len
        current_offset += size + pad_len

    total_size = ICC_HEADER_SIZE + 4 + len(tag_table_bytes) + len(data_bytes)

    new_header = bytearray(header_data)
    new_header[0:4] = struct.pack(">I", total_size)

    final_binary = new_header + struct.pack(">I", tag_count) + tag_table_bytes + data_bytes

    try:
        with output_path.open("wb") as f:
            f.write(final_binary)
    except OSError as e:
        raise ProfileWriteError(f"Failed to write output profile: {e}") from e

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

def add_or_replace_vcgt(input_path: Path, output_path: Path, table: VcgtTable) -> None:
    """
    Creates a new ICC Profile by adding or replacing the vcgt tag of the input profile.
    Maintains existing tags and ICC structural alignments.
    """
    if not input_path.exists():
        raise ProfileWriteError(f"Input file does not exist: {input_path}")

    if input_path.resolve() == output_path.resolve():
        raise ProfileWriteError("Input and output paths must not be the same to prevent data corruption.")

    # 精確捕捉 Vcgt 序列化錯誤，不使用寬鬆的 except Exception
    try:
        new_vcgt_bytes = serialize_vcgt(table)
    except InvalidVcgtError as e:
        raise ProfileWriteError(f"Failed to serialize VcgtTable: {e}") from e

    # 精確捕捉 Profile 解析錯誤
    try:
        tags = parse_profile_tags(input_path)
    except ProfileError as e:
        raise ProfileWriteError(f"Failed to parse input profile: {e}") from e

    tag_records = []
    vcgt_replaced = False

    try:
        with input_path.open("rb") as f:
            header_data = f.read(ICC_HEADER_SIZE)
            if len(header_data) < ICC_HEADER_SIZE:
                raise ProfileWriteError("Input profile header is too short.")

            for tag in tags:
                sig_bytes = tag.signature.encode("ascii")
                if sig_bytes == b"vcgt":
                    if vcgt_replaced:
                        # 拒絕靜默忽略，若原始檔案帶有多個 vcgt 則明確拋出錯誤
                        raise ProfileWriteError("Input profile contains duplicate 'vcgt' tags.")
                    tag_records.append((sig_bytes, new_vcgt_bytes))
                    vcgt_replaced = True
                else:
                    f.seek(tag.offset)
                    data = f.read(tag.size)
                    tag_records.append((sig_bytes, data))
    except OSError as e:
        raise ProfileWriteError(f"Failed to read input profile payloads: {e}") from e

    # 若原檔案中不存在 vcgt，則附加在標籤紀錄最後
    if not vcgt_replaced:
        tag_records.append((b"vcgt", new_vcgt_bytes))

    # 統一調用共用重建寫入方法，負責計算 Offset、Padding Alignment 與 Profile Size
    _write_rebuilt_profile(output_path, header_data, tag_records)

def delete_tags(input_path: Path, output_path: Path, signatures: set[str]) -> None:
    """
    Removes the specified tags from the ICC profile and writes the result to a new file.
    Non-deleted tags and their original payloads are fully preserved.
    """
    if not input_path.exists():
        raise ProfileWriteError(f"Input file does not exist: {input_path}")

    if input_path.resolve() == output_path.resolve():
        raise ProfileWriteError("Input and output paths must not be the same to prevent data corruption.")

    for sig in signatures:
        if not isinstance(sig, str) or len(sig) != 4 or not sig.isascii():
            raise ProfileWriteError(f"Invalid signature '{sig}': must be exactly 4 ASCII characters.")

    try:
        tags = parse_profile_tags(input_path)
    except ProfileError as e:
        raise ProfileWriteError(f"Failed to parse input profile: {e}") from e

    tag_records = []
    vcgt_seen = False

    try:
        with input_path.open("rb") as f:
            header_data = f.read(ICC_HEADER_SIZE)
            if len(header_data) < ICC_HEADER_SIZE:
                raise ProfileWriteError("Input profile header is too short.")

            for tag in tags:
                if tag.signature == "vcgt":
                    if vcgt_seen:
                        raise ProfileWriteError("Input profile contains duplicate 'vcgt' tags.")
                    vcgt_seen = True

                # 若標籤名列入刪除清單，則跳過，不寫入新的 tag_records
                if tag.signature in signatures:
                    continue

                sig_bytes = tag.signature.encode("ascii")
                f.seek(tag.offset)
                data = f.read(tag.size)
                tag_records.append((sig_bytes, data))
    except OSError as e:
        raise ProfileWriteError(f"Failed to read input profile payloads: {e}") from e

    # 統一調用共用重建寫入方法
    _write_rebuilt_profile(output_path, header_data, tag_records)