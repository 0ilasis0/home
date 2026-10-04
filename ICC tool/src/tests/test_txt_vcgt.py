import unittest
from pathlib import Path
from tempfile import NamedTemporaryFile

from icc import InvalidVcgtTxtError, parse_vcgt_txt, parse_vcgt_txt_content
from icc.errors import ProfileError


class TestTxtVcgtParser(unittest.TestCase):
    def _make_array(self, name, byte_str, repeat=256):
        body = ", ".join([byte_str] * repeat)
        return f"code BYTE {name}[] = {{\n{body}\n}};"

    def test_valid_txt_parsing(self):
        txt = (
            "// Some comment here\n"
            + self._make_array("tblPostGamma18_R_9300", "0x00, 0x00") + "\n"
            + self._make_array("tblPostGamma18_G_9300", "0xFF, 0x7F") + "\n"
            + self._make_array("tbl_B", "0x34, 0x12")
        )
        table = parse_vcgt_txt_content(txt)
        self.assertEqual(len(table.red), 256)
        self.assertEqual(len(table.green), 256)
        self.assertEqual(len(table.blue), 256)

    def test_known_little_endian_values(self):
        # 測試 0x34, 0x12 -> 0x1234 (Little-Endian)
        txt = (
            self._make_array("tbl_R_", "0x34, 0x12") + "\n"
            + self._make_array("tbl_G_", "0x01, 0x00") + "\n"
            + self._make_array("tbl_B_", "0xFF, 0xFF")
        )
        table = parse_vcgt_txt_content(txt)
        self.assertEqual(table.red[0], 0x1234)
        self.assertEqual(table.green[0], 0x0001)
        self.assertEqual(table.blue[0], 0xFFFF)

    def test_missing_channel(self):
        txt = self._make_array("tbl_R_", "0x00, 0x00") + "\n" + self._make_array("tbl_G_", "0x00, 0x00")
        with self.assertRaisesRegex(InvalidVcgtTxtError, "Missing BLUE channel"):
            parse_vcgt_txt_content(txt)

    def test_incorrect_and_odd_byte_count(self):
        txt = (
            self._make_array("tbl_R_", "0x00, 0x00") + "\n"
            + self._make_array("tbl_G_", "0x00, 0x00") + "\n"
            + "code BYTE tbl_B_[] = { 0x00, 0x01, 0x02 };" # 3 bytes (Odd & Incorrect)
        )
        with self.assertRaisesRegex(InvalidVcgtTxtError, "contains 3 bytes"):
            parse_vcgt_txt_content(txt)

    def test_invalid_byte_format(self):
        txt = (
            self._make_array("tbl_R_", "0x00, 0x00") + "\n"
            + self._make_array("tbl_G_", "0x00, 0x00") + "\n"
            + "code BYTE tbl_B_[] = { 0xGG, 0x00 };" # Invalid Hex
        )
        with self.assertRaisesRegex(InvalidVcgtTxtError, "Invalid hexadecimal"):
            parse_vcgt_txt_content(txt)

    def test_duplicate_channel(self):
        txt = (
            self._make_array("tbl_R_1", "0x00, 0x00") + "\n"
            + self._make_array("tbl_R_2", "0x00, 0x00") + "\n" # Duplicate
            + self._make_array("tbl_G_", "0x00, 0x00") + "\n"
            + self._make_array("tbl_B_", "0x00, 0x00")
        )
        with self.assertRaisesRegex(InvalidVcgtTxtError, "Duplicate RED channel"):
            parse_vcgt_txt_content(txt)

    def test_incorrect_byte_count(self):
        # 測試數量不足 (510 bytes)
        txt = (
            self._make_array("tbl_R_", "0x00, 0x00", repeat=255) + "\n" # 只有 510 bytes
            + self._make_array("tbl_G_", "0x00, 0x00") + "\n"
            + self._make_array("tbl_B_", "0x00, 0x00")
        )
        with self.assertRaisesRegex(InvalidVcgtTxtError, "contains 510 bytes"):
            parse_vcgt_txt_content(txt)

    def test_odd_byte_count(self):
        # 測試奇數數量 (3 bytes)
        txt = (
            self._make_array("tbl_R_", "0x00, 0x00") + "\n"
            + self._make_array("tbl_G_", "0x00, 0x00") + "\n"
            + "code BYTE tbl_B_[] = { 0x00, 0x01, 0x02 };" # 3 bytes (Odd)
        )
        with self.assertRaisesRegex(InvalidVcgtTxtError, "contains 3 bytes"):
            parse_vcgt_txt_content(txt)

    def test_parse_vcgt_txt_valid_file(self):
        txt = (
            self._make_array("tbl_R_", "0x11, 0x11") + "\n"
            + self._make_array("tbl_G_", "0x22, 0x22") + "\n"
            + self._make_array("tbl_B_", "0x33, 0x33")
        )
        with NamedTemporaryFile(mode="w", encoding="utf-8", delete=False) as temp:
            temp.write(txt)
            temp_path = Path(temp.name)

        try:
            # 測試 Public API
            table = parse_vcgt_txt(temp_path)
            self.assertEqual(len(table.red), 256)
            self.assertEqual(table.red[0], 0x1111)
        finally:
            temp_path.unlink()

    def test_parse_vcgt_txt_file_not_found(self):
        # 驗證 I/O Error Boundary
        path = Path("this_txt_does_not_exist.txt")
        with self.assertRaisesRegex(ProfileError, "File not found"):
            parse_vcgt_txt(path)

if __name__ == '__main__':
    unittest.main()