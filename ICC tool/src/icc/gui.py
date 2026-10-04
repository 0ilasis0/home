import json
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk
from typing import Optional

from .errors import ProfileError
from .models import VcgtTable
from .service import (ProfileView, import_vcgt_txt, load_profile,
                      read_vcgt_dict, save_profile)


class IccTagEditorApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("ICC Profile Tag Editor")
        self.root.geometry("600x500")

        self.input_profile: Optional[Path] = None
        self.output_profile: Optional[Path] = None
        self.current_profile_view: Optional[ProfileView] = None
        self.current_vcgt_table: Optional[VcgtTable] = None
        self.pending_deletions = set()
        self._last_attempted_path = "" # [NEW] 防止重複觸發載入

        self._build_ui()

    def _build_ui(self):
        main_frame = ttk.Frame(self.root, padding=10)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # --- Input / Output Section ---
        io_frame = ttk.Frame(main_frame)
        io_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(io_frame, text="Input:").grid(row=0, column=0, sticky=tk.W, pady=2)
        self.input_var = tk.StringVar(master=self.root)

        # [MODIFIED] 保留實體 reference 以便綁定 Event
        self.input_entry = ttk.Entry(io_frame, textvariable=self.input_var, width=50)
        self.input_entry.grid(row=0, column=1, padx=5, pady=2)
        # [NEW] 綁定 Enter 與 FocusOut 事件自動載入
        self.input_entry.bind("<Return>", self._on_enter)
        self.input_entry.bind("<FocusOut>", self._on_focusout)

        ttk.Button(io_frame, text="Browse", command=self._browse_input).grid(row=0, column=2, pady=2)

        ttk.Label(io_frame, text="Output:").grid(row=1, column=0, sticky=tk.W, pady=2)
        self.output_var = tk.StringVar(master=self.root)
        ttk.Entry(io_frame, textvariable=self.output_var, width=50).grid(row=1, column=1, padx=5, pady=2)
        ttk.Button(io_frame, text="Browse", command=self._browse_output).grid(row=1, column=2, pady=2)

        # [REMOVED] 刪除 ttk.Button(io_frame, text="Load Profile", command=self._action_load)

        # --- Tag List Section ---
        list_frame = ttk.LabelFrame(main_frame, text="Tags")
        list_frame.pack(fill=tk.BOTH, expand=True, pady=(0, 10))

        columns = ("signature", "offset", "size")
        self.tree = ttk.Treeview(list_frame, columns=columns, show="headings", selectmode="extended")
        self.tree.heading("signature", text="Signature")
        self.tree.heading("offset", text="Offset")
        self.tree.heading("size", text="Size")
        self.tree.column("signature", width=150)
        self.tree.column("offset", width=150)
        self.tree.column("size", width=150)
        self.tree.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)

        # --- Actions Section ---
        action_frame = ttk.Frame(main_frame)
        action_frame.pack(fill=tk.X, pady=(0, 10))

        self.btn_read_vcgt = ttk.Button(action_frame, text="Read vcgt", command=self._action_read_vcgt)
        self.btn_read_vcgt.grid(row=0, column=0, padx=5)
        self.btn_import_txt = ttk.Button(action_frame, text="Import vcgt TXT", command=self._action_import_txt)
        self.btn_import_txt.grid(row=0, column=1, padx=5)
        self.btn_save_profile = ttk.Button(action_frame, text="Save ICC Profile", command=self._action_save_profile)
        self.btn_save_profile.grid(row=1, column=0, padx=5, pady=5)
        self.btn_delete_tags = ttk.Button(action_frame, text="Delete Selected Tags", command=self._action_delete_tags)
        self.btn_delete_tags.grid(row=1, column=1, padx=5, pady=5)

        # --- Status Section ---
        self.status_var = tk.StringVar(master=self.root, value="Status: Ready")
        ttk.Label(main_frame, textvariable=self.status_var, relief=tk.SUNKEN, anchor=tk.W).pack(fill=tk.X, side=tk.BOTTOM)

    # --- UI Handlers ---
    def _browse_input(self):
        path = filedialog.askopenfilename(filetypes=[("ICC Profiles", "*.icc *.icm"), ("All Files", "*.*")])
        if path:
            self.input_var.set(path)
            # 來自 Browse 的操作是明確請求
            self._request_load(path, explicit=True)

    def _on_enter(self, event=None):
        path = self.input_var.get().strip()
        if path:
            # 來自 Enter 的操作是明確請求 (即使路徑相同也能 Retry)
            self._request_load(path, explicit=True)

    def _on_focusout(self, event=None):
        path = self.input_var.get().strip()
        if path:
            # 來自 FocusOut 是隱含請求，必須防止重複載入
            self._request_load(path, explicit=False)

    def _browse_output(self):
        path = filedialog.asksaveasfilename(defaultextension=".icc", filetypes=[("ICC Profiles", "*.icc *.icm")])
        if path:
            self.output_var.set(path)

    def _update_status(self, msg: str):
        self.status_var.set(f"Status: {msg}")

    def _refresh_tag_list(self):
        for item in self.tree.get_children():
            self.tree.delete(item)
        if self.current_profile_view:
            has_vcgt_visual = self.current_profile_view.has_vcgt
            for tag in self.current_profile_view.tags:
                if tag.signature not in self.pending_deletions:
                    self.tree.insert("", tk.END, values=(tag.signature, tag.offset, tag.size))
                elif tag.signature == "vcgt":
                    has_vcgt_visual = False

            vcgt_msg = " [vcgt tag present]" if has_vcgt_visual else " [No vcgt]"
            self._update_status(f"Loaded: {self.input_profile.name}{vcgt_msg}")

    # [NEW] 統一的自動載入判斷介面，避開重複讀取
    def _on_input_path_completed(self, event=None):
        current_path = self.input_var.get().strip()
        if not current_path:
            return

        last_attempt = getattr(self, "_last_attempted_path", "")
        current_loaded = str(self.input_profile) if self.input_profile else ""

        # 若路徑與當前已載入，或與最後一次嘗試載入的路徑相同，則直接返回
        if current_path == last_attempt or current_path == current_loaded:
            return

        self._last_attempted_path = current_path
        self._perform_load(Path(current_path))

    def _request_load(self, path: str, explicit: bool = True):
        last_attempt = getattr(self, "_last_attempted_path", "")
        if not explicit:
            # 防止 Browse 結束後點擊其他元件所產生的重複載入
            if path == last_attempt:
                return

        self._last_attempted_path = path
        self._perform_load(Path(path))

    def _perform_load(self, path: Path):
        try:
            new_view = load_profile(path)
            # 只有在載入"成功"時，才覆寫現有的 Profile 狀態
            self.current_profile_view = new_view
            self.input_profile = path
            self.input_var.set(str(path))

            self.pending_deletions.clear()
            self.current_vcgt_table = None

            self._refresh_tag_list()
        except ProfileError as e:
            # 載入失敗：捕捉例外、顯示錯誤，並且完全保留畫面上原有的 Profile 狀態
            messagebox.showerror("Profile Error", str(e))

    # --- Actions ---
    def _action_load(self):
        if not self.input_var.get():
            messagebox.showwarning("Warning", "Please select an input profile first.")
            return
        self._perform_load(Path(self.input_var.get()))

    def _action_read_vcgt(self):
        if not self.input_profile:
            messagebox.showwarning("Warning", "Please load a profile first.")
            return

        try:
            import json

            from .service import read_vcgt_dict

            inspection_data = read_vcgt_dict(self.input_profile)
            formatted_text = json.dumps(inspection_data, indent=4)

            top = tk.Toplevel(self.root)
            top.title(f"vcgt Binary Inspection - {self.input_profile.name}")
            top.geometry("600x600")

            # [NEW] 將佈局切割為文字區與按鈕區
            text_frame = ttk.Frame(top)
            text_frame.pack(side=tk.TOP, fill=tk.BOTH, expand=True, padx=10, pady=(10, 0))

            btn_frame = ttk.Frame(top)
            btn_frame.pack(side=tk.BOTTOM, fill=tk.X, padx=10, pady=10)

            # --- Text Area ---
            text_area = tk.Text(text_frame, wrap="none", font=("Courier", 10))
            scrollbar_y = ttk.Scrollbar(text_frame, orient="vertical", command=text_area.yview)
            scrollbar_x = ttk.Scrollbar(text_frame, orient="horizontal", command=text_area.xview)
            text_area.configure(yscrollcommand=scrollbar_y.set, xscrollcommand=scrollbar_x.set)

            scrollbar_y.pack(side=tk.RIGHT, fill=tk.Y)
            scrollbar_x.pack(side=tk.BOTTOM, fill=tk.X)
            text_area.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

            text_area.insert(tk.END, formatted_text)
            text_area.configure(state="disabled")

            # --- Export Button ---
            btn_export = ttk.Button(
                btn_frame,
                text="Convert vcgt to TXT",
                command=lambda p=self.input_profile: self._action_export_vcgt_txt(p)
            )
            btn_export.pack(side=tk.RIGHT)

        except ProfileError as e:
            messagebox.showerror("Read Error", str(e))

    def _action_export_vcgt_txt(self, profile_path: Path):
        try:
            from .service import export_vcgt_txt
            out_path = export_vcgt_txt(profile_path)
            messagebox.showinfo("Export Success", f"vcgt TXT exported successfully:\n{out_path.resolve()}")
        except ProfileError as e:
            messagebox.showerror("Export Error", str(e))

    def _action_import_txt(self):
        path = filedialog.askopenfilename(filetypes=[("Text Files", "*.txt"), ("All Files", "*.*")])
        if not path:
            return
        try:
            self.current_vcgt_table = import_vcgt_txt(Path(path))
            # [狀態覆蓋]: 若 vcgt 原先被標記刪除，匯入新資料即代表復原並覆寫它
            if "vcgt" in self.pending_deletions:
                self.pending_deletions.remove("vcgt")

            self._refresh_tag_list()
            messagebox.showinfo("Import Success", "vcgt TXT imported successfully. Ready to save.")
            self._update_status("vcgt TXT imported. Ready to save.")
        except ProfileError as e:
            messagebox.showerror("Import Error", str(e))

    def _action_delete_tags(self):
        selected = self.tree.selection()
        if not selected:
            messagebox.showwarning("Warning", "Please select at least one tag to delete.")
            return

        for item in selected:
            sig = str(self.tree.item(item)["values"][0])
            self.pending_deletions.add(sig)

        self._refresh_tag_list()
        self._update_status(f"Marked {len(self.pending_deletions)} tags for deletion. Click Save ICC Profile to commit.")

    def _action_save_profile(self):
        # 這是唯一的 Save Entry Point
        if not self.input_profile or not self.output_var.get():
            messagebox.showwarning("Warning", "Please specify both input profile and output path.")
            return

        out_path = Path(self.output_var.get())
        try:
            save_profile(self.input_profile, out_path, self.current_vcgt_table, self.pending_deletions)
            self._update_status("Profile saved successfully.")

            # Save 成功後，自動將 Output Profile 載入成為新的 Input Profile (Chaining Behavior)
            self._perform_load(out_path)
        except ProfileError as e:
            messagebox.showerror("Save Error", str(e))

def main():
    root = tk.Tk()
    app = IccTagEditorApp(root)
    root.mainloop()
