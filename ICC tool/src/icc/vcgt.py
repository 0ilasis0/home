import struct

from .errors import InvalidVcgtError, UnsupportedVcgtError
from .models import VcgtTable

VCGT_HEADER_SIZE = 18
EXPECTED_ENTRY_COUNT = 256
EXPECTED_ENTRY_SIZE = 2
EXPECTED_CHANNELS = 3
LUT_SIZE = EXPECTED_ENTRY_COUNT * EXPECTED_ENTRY_SIZE * EXPECTED_CHANNELS

def parse_vcgt(data: bytes) -> VcgtTable:
    """
    Parses a binary vcgt (Video Card Gamma Table) payload into a VcgtTable.
    Currently only supports Table Type (gammaType=0) with 3 channels and 256 entries.
    """
    if len(data) < VCGT_HEADER_SIZE:
        raise InvalidVcgtError(f"vcgt payload too short to contain a valid header: {len(data)} bytes.")

    # Unpack header: 4s(sig) 4s(reserved) I(gammaType) H(channels) H(entry_count) H(entry_size)
    sig, _, gamma_type, channels, entry_count, entry_size = struct.unpack(
        ">4s 4s I H H H", data[:VCGT_HEADER_SIZE]
    )

    try:
        sig_str = sig.decode("ascii")
    except UnicodeDecodeError:
        raise InvalidVcgtError("Invalid non-ASCII vcgt signature.")

    if sig_str != "vcgt":
        raise InvalidVcgtError(f"Invalid signature for vcgt: {sig_str}")

    if gamma_type != 0:
        raise UnsupportedVcgtError(f"Unsupported gammaType: {gamma_type}. Only Table Type (0) is supported.")

    if channels != EXPECTED_CHANNELS:
        raise InvalidVcgtError(f"Invalid channelCount: {channels}. Expected {EXPECTED_CHANNELS}.")

    if entry_count != EXPECTED_ENTRY_COUNT:
        raise InvalidVcgtError(f"Invalid entryCount: {entry_count}. Expected {EXPECTED_ENTRY_COUNT}.")

    if entry_size != EXPECTED_ENTRY_SIZE:
        raise InvalidVcgtError(f"Invalid entrySize: {entry_size}. Expected {EXPECTED_ENTRY_SIZE}.")

    expected_total_size = VCGT_HEADER_SIZE + LUT_SIZE
    if len(data) < expected_total_size:
        raise InvalidVcgtError(
            f"vcgt payload truncated. Expected at least {expected_total_size} bytes, got {len(data)}."
        )

    # Parse LUTs
    fmt = f">{EXPECTED_ENTRY_COUNT}H"
    channel_bytes = EXPECTED_ENTRY_COUNT * EXPECTED_ENTRY_SIZE

    r_offset = VCGT_HEADER_SIZE
    g_offset = r_offset + channel_bytes
    b_offset = g_offset + channel_bytes

    red = list(struct.unpack(fmt, data[r_offset : r_offset + channel_bytes]))
    green = list(struct.unpack(fmt, data[g_offset : g_offset + channel_bytes]))
    blue = list(struct.unpack(fmt, data[b_offset : b_offset + channel_bytes]))

    return VcgtTable(red=red, green=green, blue=blue)

def inspect_vcgt(data: bytes) -> dict[str, object]:
    """
    Parses a binary vcgt Table Type payload into a dictionary representation
    using fixed-width hexadecimal notation for all numeric fields.
    """
    if len(data) < VCGT_HEADER_SIZE:
        raise InvalidVcgtError(f"vcgt payload too short to contain a valid header: {len(data)} bytes.")

    sig, reserved, gamma_type, channels, entry_count, entry_size = struct.unpack(
        ">4s 4s I H H H", data[:VCGT_HEADER_SIZE]
    )

    try:
        sig_str = sig.decode("ascii")
    except UnicodeDecodeError:
        raise InvalidVcgtError("Invalid non-ASCII vcgt signature.")

    if sig_str != "vcgt":
        raise InvalidVcgtError(f"Invalid signature for vcgt: {sig_str}")

    if gamma_type != 0:
        raise UnsupportedVcgtError(f"Unsupported gammaType: {gamma_type}. Only Table Type (0) is supported.")

    if channels != EXPECTED_CHANNELS:
        raise InvalidVcgtError(f"Invalid channelCount: {channels}. Expected {EXPECTED_CHANNELS}.")
    if entry_count != EXPECTED_ENTRY_COUNT:
        raise InvalidVcgtError(f"Invalid entryCount: {entry_count}. Expected {EXPECTED_ENTRY_COUNT}.")
    if entry_size != EXPECTED_ENTRY_SIZE:
        raise InvalidVcgtError(f"Invalid entrySize: {entry_size}. Expected {EXPECTED_ENTRY_SIZE}.")

    expected_total_size = VCGT_HEADER_SIZE + LUT_SIZE
    if len(data) < expected_total_size:
        raise InvalidVcgtError(
            f"vcgt payload truncated. Expected at least {expected_total_size} bytes, got {len(data)}."
        )

    fmt = f">{EXPECTED_ENTRY_COUNT}H"
    channel_bytes = EXPECTED_ENTRY_COUNT * EXPECTED_ENTRY_SIZE

    r_offset = VCGT_HEADER_SIZE
    g_offset = r_offset + channel_bytes
    b_offset = g_offset + channel_bytes

    red_ints = struct.unpack(fmt, data[r_offset : r_offset + channel_bytes])
    green_ints = struct.unpack(fmt, data[g_offset : g_offset + channel_bytes])
    blue_ints = struct.unpack(fmt, data[b_offset : b_offset + channel_bytes])

    # Convert to fixed-width hex representations
    return {
        "signature": sig_str,
        "reserved": f"0x{int.from_bytes(reserved, 'big'):08X}",
        "gammaType": f"0x{gamma_type:08X}",
        "channels": f"0x{channels:04X}",
        "entries": f"0x{entry_count:04X}",
        "entrySize": f"0x{entry_size:04X}",
        "red": [f"0x{v:04X}" for v in red_ints],
        "green": [f"0x{v:04X}" for v in green_ints],
        "blue": [f"0x{v:04X}" for v in blue_ints],
    }