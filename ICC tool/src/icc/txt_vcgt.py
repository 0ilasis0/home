import re
from pathlib import Path

from .errors import InvalidVcgtTxtError, ProfileError
from .models import VcgtTable


def serialize_vcgt_txt(table: VcgtTable) -> str:
    """
    Serializes a VcgtTable into a C-style BYTE array TXT format.
    Each 16-bit channel entry is converted to a Little-Endian pair of bytes (0xXX).
    Outputs strictly 256 uint16 entries (512 bytes) per channel.
    """
    if len(table.red) != 256 or len(table.green) != 256 or len(table.blue) != 256:
        raise ValueError("VcgtTable channels must have exactly 256 entries.")

    lines = []
    channels = [("R", table.red), ("G", table.green), ("B", table.blue)]

    for name, data in channels:
        # [FIX] 使用字串連接替代 f-string 的 {{ 跳脫，避免語法解析錯誤
        lines.append("BYTE vcgt_" + name + "[] = {")

        hex_bytes = []
        for val in data:
            # 強制 Little-Endian 轉換：先 low byte，再 high byte
            low = val & 0xFF
            high = (val >> 8) & 0xFF
            hex_bytes.append(f"0x{low:02X}")
            hex_bytes.append(f"0x{high:02X}")

        # 每 16 個 bytes (8 個 uint16) 換行，增加可讀性
        for i in range(0, len(hex_bytes), 16):
            chunk = hex_bytes[i:i+16]
            is_last_chunk = (i + 16 >= len(hex_bytes))
            line_content = ", ".join(chunk)
            if not is_last_chunk:
                line_content += ","
            lines.append("    " + line_content)

        lines.append("};")
        lines.append("")

    return "\n".join(lines).strip()

def parse_vcgt_txt(path: Path) -> VcgtTable:
    """
    Parses a C-style BYTE array TXT file containing Red, Green, and Blue vcgt LUTs.
    """
    if not path.is_file():
        raise ProfileError(f"File not found: {path}")

    try:
        text = path.read_text(encoding="utf-8")
    except OSError as exc:
        raise ProfileError(f"Failed to read TXT file: {path}") from exc

    return parse_vcgt_txt_content(text)

def parse_vcgt_txt_content(text: str) -> VcgtTable:
    """
    Parses the text content of a TXT vcgt file into a VcgtTable.
    Expects Little-Endian 16-bit values encoded as two 8-bit hex bytes.
    """
    # 1. 過濾註解 (C-style / C++-style)
    text = re.sub(r'//.*', '', text)
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.DOTALL)

    # 2. 匹配陣列區塊 (例如: code BYTE tbl_R_9300[] = { ... })
    pattern = r'([A-Za-z0-9_]+)\s*\[\s*\]\s*=\s*\{([^}]*)\}'
    matches = re.finditer(pattern, text)

    channels = {}

    for match in matches:
        name = match.group(1)
        content = match.group(2)

        # 辨識頻道 (依據變數名稱中包含 _R_ 或是以 _R 結尾)
        ch = None
        if '_R_' in name or name.endswith('_R'):
            ch = 'red'
        elif '_G_' in name or name.endswith('_G'):
            ch = 'green'
        elif '_B_' in name or name.endswith('_B'):
            ch = 'blue'

        if not ch:
            continue

        if ch in channels:
            raise InvalidVcgtTxtError(f"Duplicate {ch.upper()} channel array found: {name}")

        # 擷取以逗號或空白分隔的 byte 字串
        raw_tokens = re.findall(r'[^,\s]+', content)
        if not raw_tokens:
            raise InvalidVcgtTxtError(f"Missing array data in {ch.upper()} channel.")

        bytes_list = []
        for t in raw_tokens:
            # 確保是嚴格的 hexadecimal byte 格式 (0x00 ~ 0xFF)
            if not re.fullmatch(r'0[xX][0-9a-fA-F]{1,2}', t):
                raise InvalidVcgtTxtError(f"Invalid hexadecimal byte value in {ch.upper()} channel: {t}")

            val = int(t, 16)
            if val > 255:
                raise InvalidVcgtTxtError(f"Byte value out of range in {ch.upper()} channel: {t}")

            bytes_list.append(val)

        if len(bytes_list) != 512:
            raise InvalidVcgtTxtError(
                f"{ch.upper()} channel contains {len(bytes_list)} bytes; expected exactly 512 bytes."
            )

        # 轉換為 Little-Endian unsigned 16-bit
        uint16_list = []
        for i in range(0, 512, 2):
            low = bytes_list[i]
            high = bytes_list[i+1]
            uint16_list.append((high << 8) | low)

        channels[ch] = uint16_list

    # 3. 驗證完整性
    for required_ch in ('red', 'green', 'blue'):
        if required_ch not in channels:
            raise InvalidVcgtTxtError(f"Missing {required_ch.upper()} channel array.")

    return VcgtTable(red=channels['red'], green=channels['green'], blue=channels['blue'])