import json
import os


def hex_to_little_endian_bytes(hex_str: str) -> list[str]:
    """
    將 Hex 字串 (例如 "0x0175") 轉換為 Little-Endian 的兩個 byte 字串 (["0x75", "0x01"])
    """
    # 將字串轉換為 16 進位整數
    val = int(hex_str, 16)

    # 提取 Low byte 與 High byte
    low_byte = val & 0xFF
    high_byte = (val >> 8) & 0xFF

    # 格式化為 0xXX 字串，並轉為大寫
    return [f"0x{low_byte:02X}", f"0x{high_byte:02X}"]

def generate_c_array(array_name: str, channel_data: list[str]) -> str:
    """
    將 channel 陣列資料轉換為 C-style array 字串格式，每行 16 bytes
    """
    byte_list = []
    # 遍歷所有的 16-bit hex 字串並轉換成 bytes
    for hex_val in channel_data:
        byte_list.extend(hex_to_little_endian_bytes(hex_val))

    # 驗證長度是否為 512 bytes
    if len(byte_list) != 512:
        raise ValueError(f"陣列 {array_name} 大小錯誤，預期 512 bytes，實際為 {len(byte_list)} bytes")

    lines = [f"static const unsigned char {array_name}[] = {{"]

    # 每 16 bytes 換行一次，方便閱讀
    for i in range(0, len(byte_list), 16):
        chunk = byte_list[i : i + 16]
        line_str = "    " + ", ".join(chunk)
        # 如果不是最後一行，加上逗號
        if i + 16 < len(byte_list):
            line_str += ","
        lines.append(line_str)

    lines.append("};")
    return "\n".join(lines)

def convert_vcgt_json_to_txt(input_path: str, output_path: str):
    """
    主轉換流程：讀取 JSON 檔案，解析 RGB channel 並輸出為 TXT
    """
    print(f"正在讀取輸入檔案: {input_path}")
    with open(input_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # 確保 RGB channel 都存在
    for channel in ["red", "green", "blue"]:
        if channel not in data:
            raise KeyError(f"輸入檔案中找不到 '{channel}' channel 的資料！")

    print("開始轉換並產生 C-style BYTE arrays (Little-Endian)...")

    # 產生各 Channel 的 C-style 陣列
    r_array_str = generate_c_array("_R_", data["red"])
    g_array_str = generate_c_array("_G_", data["green"])
    b_array_str = generate_c_array("_B_", data["blue"])

    # 組合最終檔案內容，以兩個換行符號隔開
    final_output = f"{r_array_str}\n\n{g_array_str}\n\n{b_array_str}\n"

    # 寫入輸出檔案
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(final_output)

    print(f"轉換成功！已將結果儲存至: {output_path}")

if __name__ == "__main__":
    # ==========================================
    # 在這裡修改您的輸入與輸出檔案路徑！
    # ==========================================

    INPUT_JSON_PATH = r"C:\Users\User\Desktop\vcgt.txt"  # 您提供的 JSON/Dict 格式檔案路徑
    OUTPUT_TXT_PATH = r"C:\Users\User\Desktop\output_trans.txt"    # 轉換後的 TXT 輸出檔案路徑

    # ==========================================

    # 簡易測試環境設定：如果找不到檔案，提醒使用者建立
    if not os.path.exists(INPUT_JSON_PATH):
        print(f"[錯誤] 找不到檔案 '{INPUT_JSON_PATH}'。")
        print("請先將您要轉換的 dict 資料存成 JSON 檔案，並正確設定輸入路徑。")
    else:
        try:
            convert_vcgt_json_to_txt(INPUT_JSON_PATH, OUTPUT_TXT_PATH)
        except Exception as e:
            print(f"[發生錯誤] 轉換失敗: {e}")