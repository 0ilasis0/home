import struct
import unittest
from pathlib import Path
from tempfile import NamedTemporaryFile
from unittest.mock import MagicMock, patch

from icc.constants import ICC_HEADER_SIZE
from icc.errors import ProfileError
from icc.models import VcgtTable
from icc.service import (delete_profile_tags, import_vcgt_txt, load_profile,
                         read_vcgt, save_vcgt)
from icc.writer import serialize_vcgt


class TestServiceLayer(unittest.TestCase):
    def _create_mock_profile(self, tags: list[tuple[bytes, int, int]], payload_bytes: bytes) -> bytes:
        header = b"\x00" * ICC_HEADER_SIZE
        tag_count = struct.pack(">I", len(tags))
        tag_table = bytearray()
        for sig, offset, size in tags:
            tag_table += struct.pack(">4sII", sig, offset, size)
        return header + tag_count + tag_table + payload_bytes

    # --- Test 1 to 3: Load Profile ---
    def test_load_profile_with_vcgt(self):
        # [FIX REQUIRED 2]: 修正 Tag offset 避免與 Tag Table (長度 168 bytes) 重疊
        # Header(128) + Count(4) + 3*12(36) = 168
        tags_def = [(b"cprt", 168, 10), (b"desc", 178, 10), (b"vcgt", 188, 20)]
        mock_data = self._create_mock_profile(tags_def, b"\x00" * 40)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            view = load_profile(temp_path)
            self.assertEqual(view.path, temp_path)
            self.assertEqual(len(view.tags), 3)
            self.assertTrue(view.has_vcgt)
            # Test 3: Preserves TagInfo
            self.assertEqual(view.tags[2].signature, "vcgt")
            self.assertEqual(view.tags[2].offset, 188)
            self.assertEqual(view.tags[2].size, 20)
        finally:
            temp_path.unlink()

    def test_load_profile_without_vcgt(self):
        # Header(128) + Count(4) + 1*12(12) = 144
        tags_def = [(b"cprt", 144, 10)]
        mock_data = self._create_mock_profile(tags_def, b"\x00" * 10)
        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            view = load_profile(temp_path)
            self.assertFalse(view.has_vcgt)
            self.assertEqual(len(view.tags), 1)
        finally:
            temp_path.unlink()

    # --- Test 4 to 5: Read vcgt ---
    def test_read_vcgt_valid(self):
        table = VcgtTable(red=[1]*256, green=[2]*256, blue=[3]*256)
        vcgt_payload = serialize_vcgt(table)
        payload_size = len(vcgt_payload)

        # Header(128) + Count(4) + 2*12(24) = 156
        tags_def = [(b"cprt", 156, 10), (b"vcgt", 166, payload_size)]
        mock_data = self._create_mock_profile(tags_def, b"\x00"*10 + vcgt_payload)

        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            result_table = read_vcgt(temp_path)
            self.assertEqual(result_table.red[0], 1)
            self.assertEqual(result_table.green[0], 2)
            self.assertEqual(result_table.blue[0], 3)
        finally:
            temp_path.unlink()

    def test_read_vcgt_missing(self):
        tags_def = [(b"cprt", 144, 10)]
        mock_data = self._create_mock_profile(tags_def, b"\x00" * 10)
        with NamedTemporaryFile(delete=False) as temp:
            temp.write(mock_data)
            temp_path = Path(temp.name)

        try:
            with self.assertRaisesRegex(ProfileError, "No 'vcgt' tag found"):
                read_vcgt(temp_path)
        finally:
            temp_path.unlink()

    # --- Test 6 to 9: Write & Delete (Delegation + Behavior) ---
    @patch('icc.service.parse_vcgt_txt')
    def test_import_vcgt_txt_delegation(self, mock_parse):
        mock_table = MagicMock()
        mock_parse.return_value = mock_table
        path = Path("dummy.txt")
        result = import_vcgt_txt(path)
        mock_parse.assert_called_once_with(path)
        self.assertEqual(result, mock_table)

    @patch('icc.service.add_or_replace_vcgt')
    def test_save_vcgt_delegation(self, mock_add):
        table = MagicMock()
        in_path, out_path = Path("in.icc"), Path("out.icc")
        save_vcgt(in_path, out_path, table)
        mock_add.assert_called_once_with(in_path, out_path, table)

    # [FIX REQUIRED 3]: 補 service behavioral tests (Save -> Read back)
    def test_save_vcgt_behavior(self):
        tags_def = [(b"cprt", 144, 10)]
        mock_data = self._create_mock_profile(tags_def, b"\x00" * 10)

        with NamedTemporaryFile(delete=False) as temp_in, NamedTemporaryFile(delete=False) as temp_out:
            temp_in.write(mock_data)
            in_path = Path(temp_in.name)
            out_path = Path(temp_out.name)

        try:
            table = VcgtTable(red=[0x1111]*256, green=[0x2222]*256, blue=[0x3333]*256)
            save_vcgt(in_path, out_path, table)

            # 使用 Service API 讀回並驗證
            result_table = read_vcgt(out_path)
            self.assertEqual(result_table.red[0], 0x1111)
            self.assertEqual(result_table.green[0], 0x2222)
            self.assertEqual(result_table.blue[0], 0x3333)
        finally:
            in_path.unlink(missing_ok=True)
            out_path.unlink(missing_ok=True)

    @patch('icc.service.delete_tags')
    def test_delete_profile_tags_delegation(self, mock_delete):
        in_path, out_path = Path("in.icc"), Path("out.icc")
        sigs = {"cprt"}
        delete_profile_tags(in_path, out_path, sigs)
        mock_delete.assert_called_once_with(in_path, out_path, sigs)

    # [FIX REQUIRED 3]: 補 service behavioral tests (Delete -> Load Profile)
    def test_delete_profile_tags_behavior(self):
        tags_def = [(b"cprt", 168, 10), (b"desc", 178, 10), (b"vcgt", 188, 20)]
        mock_data = self._create_mock_profile(tags_def, b"\x00" * 40)

        with NamedTemporaryFile(delete=False) as temp_in, NamedTemporaryFile(delete=False) as temp_out:
            temp_in.write(mock_data)
            in_path = Path(temp_in.name)
            out_path = Path(temp_out.name)

        try:
            # 刪除 desc 與 vcgt
            delete_profile_tags(in_path, out_path, {"desc", "vcgt"})

            # 使用 Service API 重新載入，確認被成功刪除
            view = load_profile(out_path)
            self.assertEqual(len(view.tags), 1)
            self.assertEqual(view.tags[0].signature, "cprt")
            self.assertFalse(view.has_vcgt)
        finally:
            in_path.unlink(missing_ok=True)
            out_path.unlink(missing_ok=True)

    # --- Test 10: Error Propagation ---
    def test_error_propagation(self):
        with self.assertRaisesRegex(ProfileError, "File not found"):
            load_profile(Path("does_not_exist.icc"))

if __name__ == '__main__':
    unittest.main()