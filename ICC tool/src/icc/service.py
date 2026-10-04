from dataclasses import dataclass
from pathlib import Path

from .errors import ProfileError
from .models import TagInfo, VcgtTable
from .parser import parse_profile_tags, read_tag_payload
from .txt_vcgt import parse_vcgt_txt
from .vcgt import parse_vcgt
from .writer import add_or_replace_vcgt, delete_tags


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
    Raises ProfileError if the tag is missing.
    """
    tags = parse_profile_tags(profile_path)
    vcgt_tag = next((tag for tag in tags if tag.signature == "vcgt"), None)

    if vcgt_tag is None:
        raise ProfileError(f"No 'vcgt' tag found in profile: {profile_path}")

    payload = read_tag_payload(profile_path, vcgt_tag)
    return parse_vcgt(payload)


def import_vcgt_txt(txt_path: Path) -> VcgtTable:
    """
    Delegates to the TXT parser to extract a VcgtTable from a C-style array text file.
    """
    return parse_vcgt_txt(txt_path)


def save_vcgt(input_profile: Path, output_profile: Path, table: VcgtTable) -> None:
    """
    Delegates to the ICC writer to add or replace the vcgt tag in a profile.
    """
    add_or_replace_vcgt(input_profile, output_profile, table)


def delete_profile_tags(input_profile: Path, output_profile: Path, signatures: set[str]) -> None:
    """
    Delegates to the ICC writer to remove the specified tags from a profile.
    """
    delete_tags(input_profile, output_profile, signatures)