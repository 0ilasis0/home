import tkinter as tk
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

from icc.errors import InvalidProfileError
from icc.gui import IccTagEditorApp
from icc.models import TagInfo, VcgtTable
from icc.service import ProfileView


# [FIX] 修正測試 Fixture，建立合法的 256-entry table 以避免潛在的 serialzer 嚴格檢查失敗
def create_valid_mock_vcgt() -> VcgtTable:
    return VcgtTable(red=[0]*256, green=[0]*256, blue=[0]*256)

class TestGuiApp(unittest.TestCase):
    def setUp(self):
        # 註: 此 Tk() 的實例化需要作業系統提供 Display Environment (如 Windows GUI, MacOS, 或 Linux 的 X11/Wayland/Xvfb)
        self.root = tk.Tk()
        self.app = IccTagEditorApp(self.root)

    def tearDown(self):
        self.root.destroy()

    # --- [NEW] Input / Output Browse Behavioral Tests ---
    @patch('icc.gui.filedialog.askopenfilename')
    def test_browse_input_action(self, mock_ask):
        mock_ask.return_value = "mock_input.icc"
        self.app._browse_input()
        self.assertEqual(self.app.input_var.get(), "mock_input.icc")

    @patch('icc.gui.filedialog.asksaveasfilename')
    def test_browse_output_action(self, mock_ask):
        mock_ask.return_value = "mock_output.icc"
        self.app._browse_output()
        self.assertEqual(self.app.output_var.get(), "mock_output.icc")

    # --- State Loading & Transitions ---
    @patch('icc.gui.load_profile')
    def test_perform_load_updates_input_state(self, mock_load):
        # 驗證 GUI state transfer (Save/Delete 之後的核心更新邏輯)
        mock_view = ProfileView(Path("new_state.icc"), (), False)
        mock_load.return_value = mock_view

        self.app._perform_load(Path("new_state.icc"))

        self.assertEqual(self.app.input_profile, Path("new_state.icc"))
        self.assertEqual(self.app.input_var.get(), "new_state.icc")
        self.assertEqual(self.app.current_profile_view, mock_view)

    @patch('icc.gui.load_profile')
    def test_load_action_updates_state(self, mock_load):
        tags = (TagInfo("cprt", 100, 10), TagInfo("vcgt", 110, 20))
        mock_view = ProfileView(Path("dummy.icc"), tags, True)
        mock_load.return_value = mock_view

        self.app.input_var.set("dummy.icc")
        self.app._action_load()

        mock_load.assert_called_once_with(Path("dummy.icc"))
        self.assertEqual(self.app.current_profile_view, mock_view)

        tree_items = self.app.tree.get_children()
        self.assertEqual(len(tree_items), 2)
        self.assertEqual(self.app.tree.item(tree_items[1])["values"][0], "vcgt")

    # --- [NEW] Read vcgt Behavioral Test ---
    @patch('icc.gui.read_vcgt')
    @patch('icc.gui.messagebox.showinfo')
    def test_read_vcgt_action(self, mock_info, mock_read):
        self.app.input_profile = Path("dummy.icc")
        mock_read.return_value = create_valid_mock_vcgt()

        self.app._action_read_vcgt()

        mock_read.assert_called_once_with(Path("dummy.icc"))
        mock_info.assert_called_once()
        # 驗證 messagebox.showinfo 的內容參數是否包含正確資訊
        self.assertIn("Channels: 3", mock_info.call_args[0][1])
        self.assertIn("Entries per channel: 256", mock_info.call_args[0][1])

    # --- Actions and Delegations ---
    @patch('icc.gui.filedialog.askopenfilename')
    @patch('icc.gui.import_vcgt_txt')
    @patch('icc.gui.messagebox.showinfo')
    def test_import_txt_action(self, mock_info, mock_import, mock_ask):
        mock_ask.return_value = "dummy.txt"
        mock_table = create_valid_mock_vcgt()
        mock_import.return_value = mock_table

        self.app._action_import_txt()

        mock_import.assert_called_once_with(Path("dummy.txt"))
        self.assertEqual(self.app.current_vcgt_table, mock_table)

    @patch('icc.gui.save_vcgt')
    @patch('icc.gui.IccTagEditorApp._perform_load')
    def test_save_action_and_refresh(self, mock_load, mock_save):
        self.app.input_profile = Path("in.icc")
        self.app.output_var.set("out.icc")
        self.app.current_vcgt_table = create_valid_mock_vcgt()

        self.app._action_save_vcgt()

        mock_save.assert_called_once_with(Path("in.icc"), Path("out.icc"), self.app.current_vcgt_table)
        mock_load.assert_called_once_with(Path("out.icc"))

    @patch('icc.gui.delete_profile_tags')
    @patch('icc.gui.IccTagEditorApp._perform_load')
    def test_delete_action_and_refresh(self, mock_load, mock_delete):
        self.app.input_profile = Path("in.icc")
        self.app.output_var.set("out.icc")

        item_id = self.app.tree.insert("", tk.END, values=("cprt", 100, 10))
        self.app.tree.selection_set(item_id)

        self.app._action_delete_tags()

        mock_delete.assert_called_once_with(Path("in.icc"), Path("out.icc"), {"cprt"})
        mock_load.assert_called_once_with(Path("out.icc"))

    @patch('icc.gui.delete_profile_tags')
    @patch('icc.gui.messagebox.showwarning')
    def test_empty_selection_prevents_delete(self, mock_warning, mock_delete):
        self.app.input_profile = Path("in.icc")
        self.app.output_var.set("out.icc")

        self.app._action_delete_tags()

        mock_delete.assert_not_called()
        mock_warning.assert_called_once()

    @patch('icc.gui.load_profile')
    @patch('icc.gui.messagebox.showerror')
    def test_error_handling_propagates_to_gui(self, mock_error, mock_load):
        # 確保 ProfileError (與其子類別) 能夠正常轉換為 messagebox
        mock_load.side_effect = InvalidProfileError("Malformed ICC header")

        self.app.input_var.set("bad.icc")
        self.app._action_load()

        mock_error.assert_called_once_with("Profile Error", "Malformed ICC header")

if __name__ == '__main__':
    unittest.main()