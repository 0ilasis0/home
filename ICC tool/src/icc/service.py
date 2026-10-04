from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from .errors import ProfileError
from .models import TagInfo, VcgtTable
from .parser import parse_profile_tags, read_tag_payload
from .txt_vcgt import parse_vcgt_txt, serialize_vcgt_txt
from .vcgt import inspect_vcgt, parse_vcgt
from .writer import add_or_replace_vcgt, delete_tags
from .writer import save_profile as writer_save_profile


@dataclass(frozen=True)
class ProfileView:
    """GUI-facing representation of an ICC Profile overview."""
    path: Path
    tags: tuple[TagInfo, ...]
    has_vcgt: bool


def load_profile(profile_path: Path) -> ProfileView:
    """
    Loads an overview of the ICC profile, extracting tag metadata and
    determining if a 'vcgt' tag is present.
    """
    tags_list = parse_profile_tags(profile_path)
    tags_tuple = tuple(tags_list)
    has_vcgt = any(tag.signature == "vcgt" for tag in tags_tuple)

    return ProfileView(
        path=profile_path,
        tags=tags_tuple,
        has_vcgt=has_vcgt
    )


def read_vcgt(profile_path: Path) -> VcgtTable:
    """
    Locates and parses the 'vcgt' payload from the given profile.
    Returns the core VcgtTable model.
    Raises ProfileError if the tag is missing.
    """
    tags = parse_profile_tags(profile_path)
    vcgt_tag = next((tag for tag in tags if tag.signature == "vcgt"), None)

    if vcgt_tag is None:
        raise ProfileError(f"No 'vcgt' tag found in profile: {profile_path}")

    payload = read_tag_payload(profile_path, vcgt_tag)
    return parse_vcgt(payload)


def read_vcgt_dict(profile_path: Path) -> dict[str, object]:
    """
    Locates and parses the 'vcgt' payload into a hex-formatted inspection dictionary.
    Raises ProfileError if the tag is missing.
    """
    tags = parse_profile_tags(profile_path)
    vcgt_tag = next((tag for tag in tags if tag.signature == "vcgt"), None)

    if vcgt_tag is None:
        raise ProfileError(f"No 'vcgt' tag found in profile: {profile_path}")

    payload = read_tag_payload(profile_path, vcgt_tag)
    return inspect_vcgt(payload)


def import_vcgt_txt(txt_path: Path) -> VcgtTable:
    """
    Delegates to the TXT parser to extract a VcgtTable from a C-style array text file.
    """
    return parse_vcgt_txt(txt_path)


def export_vcgt_txt(profile_path: Path) -> Path:
    """
    Reads the vcgt payload from the given profile, converts it to Little-Endian TXT format,
    and exports it as 'vcgt_trans.txt' in the same directory as the input profile.
    Overwrites the file deterministically if it already exists.
    """
    table = read_vcgt(profile_path)
    txt_content = serialize_vcgt_txt(table)

    out_path = profile_path.parent / "vcgt_trans.txt"
    try:
        out_path.write_text(txt_content, encoding="utf-8")
    except OSError as e:
        raise ProfileError(f"Failed to write exported TXT: {e}") from e

    return out_path


# ==========================================
# Write & Edit Operations
# ==========================================

def save_vcgt(input_profile: Path, output_profile: Path, table: VcgtTable) -> None:
    """
    Delegates to the ICC writer to add or replace the vcgt tag in a profile.
    (Legacy API, preserved for backward compatibility)
    """
    add_or_replace_vcgt(input_profile, output_profile, table)


def delete_profile_tags(input_profile: Path, output_profile: Path, signatures: set[str]) -> None:
    """
    Delegates to the ICC writer to remove the specified tags from a profile.
    (Legacy API, preserved for backward compatibility)
    """
    delete_tags(input_profile, output_profile, signatures)


def save_profile(
    input_profile: Path,
    output_profile: Path,
    vcgt_table: Optional[VcgtTable] = None,
    tags_to_delete: Optional[set[str]] = None
) -> None:
    """
    Delegates to the ICC writer to save the ICC profile state.
    Commits pending tag deletions and/or vcgt modifications.
    Untouched tags (including vcgt if not modified) are preserved byte-for-byte.
    If no modifications are requested, performs a byte-for-byte no-op copy.
    """
    writer_save_profile(input_profile, output_profile, vcgt_table, tags_to_delete)