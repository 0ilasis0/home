import struct
from pathlib import Path

from .constants import ICC_HEADER_SIZE, TAG_COUNT_SIZE, TAG_ENTRY_SIZE
from .errors import InvalidProfileError, ProfileError
from .models import TagInfo


def parse_profile_tags(profile_path: Path) -> list[TagInfo]:
    """
    Reads an ICC Profile and extracts all Tag table entries.

    Args:
        profile_path: Path to the ICC Profile file.

    Returns:
        A list of TagInfo objects representing the tags in the profile.

    Raises:
        ProfileError: If the file cannot be found or read.
        InvalidProfileError: If the ICC profile structure is malformed.
    """
    if not profile_path.is_file():
        raise ProfileError(f"File not found or is not a regular file: {profile_path}")

    try:
        data = profile_path.read_bytes()
    except OSError as exc:
        raise ProfileError(f"Failed to read ICC profile: {profile_path}") from exc

    file_size = len(data)

    # 驗證 1: 檔案大小是否能容納 Header + Tag Count
    if file_size < ICC_HEADER_SIZE + TAG_COUNT_SIZE:
        raise InvalidProfileError("File size is too small to contain a valid ICC header and tag count.")

    # ICC 資料皆為 Big-Endian
    tag_count_data = data[ICC_HEADER_SIZE : ICC_HEADER_SIZE + TAG_COUNT_SIZE]
    tag_count = struct.unpack(">I", tag_count_data)[0]

    expected_table_size = tag_count * TAG_ENTRY_SIZE

    # 驗證 2: 檔案大小是否能容納所宣告的 Tag Table
    if file_size < ICC_HEADER_SIZE + TAG_COUNT_SIZE + expected_table_size:
        raise InvalidProfileError("Tag table definition exceeds actual file size.")

    tags = []
    table_offset = ICC_HEADER_SIZE + TAG_COUNT_SIZE

    for i in range(tag_count):
        entry_start = table_offset + (i * TAG_ENTRY_SIZE)
        entry_data = data[entry_start : entry_start + TAG_ENTRY_SIZE]

        sig_bytes, offset, size = struct.unpack(">4sII", entry_data)

        try:
            sig = sig_bytes.decode("ascii")
        except UnicodeDecodeError:
            raise InvalidProfileError(f"Invalid non-ASCII tag signature at index {i}.")

        # 驗證 3: Tag 內容的 offset 與 size 是否越界 (超出檔案大小)
        if offset + size > file_size:
            raise InvalidProfileError(
                f"Tag '{sig}' payload exceeds file boundaries "
                f"(offset: {offset}, size: {size}, file_size: {file_size})."
            )

        tags.append(TagInfo(signature=sig, offset=offset, size=size))

    return tags

def read_tag_payload(profile_path: Path, tag: TagInfo) -> bytes:
    """
    Reads the raw binary payload of a specific ICC tag from the profile.
    """
    if not profile_path.is_file():
        raise ProfileError(f"File not found: {profile_path}")

    try:
        with profile_path.open("rb") as f:
            f.seek(tag.offset)
            payload = f.read(tag.size)
            if len(payload) < tag.size:
                raise InvalidProfileError(
                    f"File truncated while reading tag payload (expected {tag.size}, got {len(payload)})."
                )
            return payload
    except OSError as exc:
        raise ProfileError(f"Failed to read tag payload from: {profile_path}") from exc
