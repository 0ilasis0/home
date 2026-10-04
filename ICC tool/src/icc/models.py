from dataclasses import dataclass


@dataclass
class TagInfo:
    signature: str
    offset: int
    size: int

@dataclass
class VcgtTable:
    """Represents the extracted RGB LUTs from a vcgt tag."""
    red: list[int]
    green: list[int]
    blue: list[int]