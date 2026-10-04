import struct
import unittest
from pathlib import Path
from tempfile import NamedTemporaryFile

from icc import InvalidProfileError, ProfileError, parse_profile_tags
from icc.constants import ICC_HEADER_SIZE
from utils.debug import dbg


class TestICCParser(unittest.TestCase):

    def _create_mock_profile(self, tags: list[tuple[bytes, int, int]], payload_size: int = 0) -> bytes:
        """Helper to generate a mock ICC profile binary structure."""
        dbg.log(f"Generating mock profile with {len(tags)} tags, payload_size={payload_size}")
        header = b"\x00" * ICC_HEADER_SIZE
        tag_count = struct.pack(">I", len(tags))

        tag_table = bytearray()
        for sig, offset, size in tags:
            tag_table += struct.pack(">4sII", sig, offset, size)

        payload = b"\x00" * payload_size
        total_data = header + tag_count + tag_table + payload
        dbg.var(total_generated_bytes=len(total_data))
        return total_data

    def test_parse_valid_profile_with_vcgt(self):
        dbg.log("=== START: test_parse_valid_profile_with_vcgt ===")
        tags_def = [
            (b"cprt", 156, 10),
            (b"vcgt", 166, 20),
        ]
        mock_data = self._create_mock_profile(tags_def, payload_size=30)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            tags = parse_profile_tags(temp_path)
            dbg.dump([t.__dict__ for t in tags], label="Parsed Tags in test_parse_valid_profile_with_vcgt")

            self.assertEqual(len(tags), 2)
            self.assertEqual(tags[0].signature, "cprt")
            self.assertEqual(tags[0].offset, 156)
            self.assertEqual(tags[0].size, 10)

            self.assertEqual(tags[1].signature, "vcgt")
            self.assertEqual(tags[1].offset, 166)
            self.assertEqual(tags[1].size, 20)
        finally:
            temp_path.unlink()
            dbg.log("Temporary file cleaned up.")

    def test_parse_valid_profile_without_vcgt(self):
        tags_def = [(b"cprt", 156, 10)]

        # [FIX] 原本 payload_size=10 會讓總檔案大小只有 154 (128+4+12+10)
        # 現在將 payload_size 改為 22，讓總檔案大小達到 166 bytes (144 + 22)
        # 剛好能容納 offset(156) + size(10) = 166 的邊界需求
        mock_data = self._create_mock_profile(tags_def, payload_size=22)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            tags = parse_profile_tags(temp_path)
            self.assertEqual(len(tags), 1)
            # AC-004 驗證：只有 cprt，不會誤認為包含 vcgt
            self.assertEqual(tags[0].signature, "cprt")
        finally:
            temp_path.unlink()

    def test_file_not_found(self):
        dbg.log("=== START: test_file_not_found ===")
        non_existent_file = Path("non_existent_test_file.icc")
        with self.assertRaises(ProfileError) as cm:
            parse_profile_tags(non_existent_file)
        dbg.log(f"Verified expected exception caught: {cm.exception}")

    def test_file_too_small(self):
        dbg.log("=== START: test_file_too_small ===")
        mock_data = b"\x00" * 50
        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            with self.assertRaises(InvalidProfileError) as cm:
                parse_profile_tags(temp_path)
            dbg.log(f"Verified expected exception caught: {cm.exception}")
        finally:
            temp_path.unlink()
            dbg.log("Temporary file cleaned up.")

    def test_tag_table_truncated(self):
        dbg.log("=== START: test_tag_table_truncated ===")
        mock_data = b"\x00" * ICC_HEADER_SIZE + struct.pack(">I", 10)
        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            with self.assertRaises(InvalidProfileError) as cm:
                parse_profile_tags(temp_path)
            dbg.log(f"Verified expected exception caught: {cm.exception}")
        finally:
            temp_path.unlink()
            dbg.log("Temporary file cleaned up.")

    def test_tag_exceeds_bounds(self):
        dbg.log("=== START: test_tag_exceeds_bounds ===")
        tags_def = [(b"cprt", 156, 100)]
        mock_data = self._create_mock_profile(tags_def, payload_size=10)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            with self.assertRaises(InvalidProfileError) as cm:
                parse_profile_tags(temp_path)
            dbg.log(f"Verified expected exception caught: {cm.exception}")
        finally:
            temp_path.unlink()
            dbg.log("Temporary file cleaned up.")

    def test_empty_file(self):
        # 測試完全空白的檔案 (AC-005)
        with NamedTemporaryFile(delete=False) as temp:
            pass  # 不寫入任何資料，建立 0 bytes 檔案
            temp_path = Path(temp.name)

        try:
            with self.assertRaises(InvalidProfileError) as context:
                parse_profile_tags(temp_path)
            # 驗證例外訊息是否符合預期
            self.assertIn("too small", str(context.exception))
        finally:
            temp_path.unlink()

    def test_non_ascii_signature(self):
        # 測試包含非 ASCII 字元的 signature，例如 "\xff\xff\xff\xff" (AC-005)
        tags_def = [(b"\xff\xff\xff\xff", 156, 10)]
        mock_data = self._create_mock_profile(tags_def, payload_size=10)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            with self.assertRaises(InvalidProfileError) as context:
                parse_profile_tags(temp_path)
            # 驗證例外訊息是否準確捕捉到 signature 解析失敗
            self.assertIn("Invalid non-ASCII tag signature", str(context.exception))
        finally:
            temp_path.unlink()

if __name__ == '__main__':
    unittest.main()