import tkinter as tk
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

from icc.errors import InvalidProfileError
from icc.gui import IccTagEditorApp
from icc.models import TagInfo, VcgtTable
from icc.service import ProfileView


def create_valid_mock_vcgt() -> VcgtTable:
    return VcgtTable(red=[0]*256, green=[0]*256, blue=[0]*256)

class TestGuiApp(unittest.TestCase):
    def setUp(self):
        self.root = tk.Tk()
        self.app = IccTagEditorApp(self.root)

    def tearDown(self):
        # [FIX Problem C] 清除殘留事件，安全釋放 Tk 資源
        self.root.update_idletasks()
        self.root.destroy()

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

    @patch('icc.gui.load_profile')
    def test_perform_load_updates_input_state(self, mock_load):
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

    @patch('icc.gui.read_vcgt')
    @patch('icc.gui.messagebox.showinfo')
    def test_read_vcgt_action(self, mock_info, mock_read):
        self.app.input_profile = Path("dummy.icc")
        mock_read.return_value = create_valid_mock_vcgt()

        self.app._action_read_vcgt()
        mock_read.assert_called_once_with(Path("dummy.icc"))
        mock_info.assert_called_once()

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

    def test_gui_has_no_legacy_save_vcgt_button(self):
        # [FIX Problem B] 驗證完全沒有遺留舊按鈕與 Handler
        self.assertEqual(self.app.btn_save_profile.cget("text"), "Save ICC Profile")
        self.assertFalse(hasattr(self.app, "_action_save_vcgt"))
        action_frame = self.app.btn_save_profile.master
        for widget in action_frame.winfo_children():
            if isinstance(widget, tk.ttk.Button):
                self.assertNotEqual(widget.cget("text"), "Save vcgt")

    @patch('icc.gui.save_profile')
    @patch('icc.gui.IccTagEditorApp._perform_load')
    def test_save_profile_button_wiring(self, mock_load, mock_save_profile):
        # [FIX Problem A] 透過 .invoke() 真實驗證按鈕行為與委派
        self.app.input_profile = Path("in.icc")
        self.app.output_var.set("out.icc")
        self.app.current_vcgt_table = create_valid_mock_vcgt()
        self.app.pending_deletions = {"desc"}

        # 模擬實體按鈕點擊
        self.app.btn_save_profile.invoke()

        # 驗證真的走到了唯一的 save entry point
        mock_save_profile.assert_called_once_with(
            Path("in.icc"),
            Path("out.icc"),
            self.app.current_vcgt_table,
            {"desc"}
        )
        mock_load.assert_called_once_with(Path("out.icc"))

    def test_delete_action_updates_pending_state(self):
        # [FIX Problem B] 修正 Delete 的驗證，不直接觸發儲存，而是驗證狀態修改
        tags = (TagInfo("cprt", 100, 10), TagInfo("desc", 110, 20))
        self.app.current_profile_view = ProfileView(Path("in.icc"), tags, False)
        self.app._refresh_tag_list()

        items = self.app.tree.get_children()
        desc_item = next(i for i in items if self.app.tree.item(i)["values"][0] == "desc")
        self.app.tree.selection_set(desc_item)

        self.app._action_delete_tags()

        self.assertIn("desc", self.app.pending_deletions)
        items_after = self.app.tree.get_children()
        self.assertEqual(len(items_after), 1)
        self.assertEqual(self.app.tree.item(items_after[0])["values"][0], "cprt")

    @patch('icc.gui.messagebox.showwarning')
    def test_empty_selection_prevents_delete(self, mock_warning):
        self.app.input_profile = Path("in.icc")
        self.app.output_var.set("out.icc")

        self.app._action_delete_tags()
        mock_warning.assert_called_once()
        self.assertEqual(len(self.app.pending_deletions), 0)

    @patch('icc.gui.load_profile')
    @patch('icc.gui.messagebox.showerror')
    def test_error_handling_propagates_to_gui(self, mock_error, mock_load):
        mock_load.side_effect = InvalidProfileError("Malformed ICC header")

        self.app.input_var.set("bad.icc")
        self.app._action_load()

        mock_error.assert_called_once_with("Profile Error", "Malformed ICC header")

if __name__ == '__main__':
    unittest.main()