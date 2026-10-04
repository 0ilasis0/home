import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk
from typing import Optional

from .errors import ProfileError
from .models import VcgtTable
from .service import (ProfileView, import_vcgt_txt, load_profile, read_vcgt,
                      save_profile)


class IccTagEditorApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("ICC Profile Tag Editor")
        self.root.geometry("600x500")

        self.input_profile: Optional[Path] = None
        self.output_profile: Optional[Path] = None
        self.current_profile_view: Optional[ProfileView] = None
        self.current_vcgt_table: Optional[VcgtTable] = None

        # 紀錄待刪除標籤的編輯狀態 (TASK-009)
        self.pending_deletions = set()

        self._build_ui()

    def _build_ui(self):
        main_frame = ttk.Frame(self.root, padding=10)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # --- Input / Output Section ---
        io_frame = ttk.Frame(main_frame)
        io_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(io_frame, text="Input:").grid(row=0, column=0, sticky=tk.W, pady=2)
        self.input_var = tk.StringVar(master=self.root)
        ttk.Entry(io_frame, textvariable=self.input_var, width=50).grid(row=0, column=1, padx=5, pady=2)
        ttk.Button(io_frame, text="Browse", command=self._browse_input).grid(row=0, column=2, pady=2)

        ttk.Label(io_frame, text="Output:").grid(row=1, column=0, sticky=tk.W, pady=2)
        self.output_var = tk.StringVar(master=self.root)
        ttk.Entry(io_frame, textvariable=self.output_var, width=50).grid(row=1, column=1, padx=5, pady=2)
        ttk.Button(io_frame, text="Browse", command=self._browse_output).grid(row=1, column=2, pady=2)

        ttk.Button(io_frame, text="Load Profile", command=self._action_load).grid(row=2, column=1, pady=5)

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

        # (TASK-010: 明確建立按鈕實體並綁定，徹底移除舊有 "Save vcgt")
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
                # 過濾掉目前已被標記準備刪除的標籤
                if tag.signature not in self.pending_deletions:
                    self.tree.insert("", tk.END, values=(tag.signature, tag.offset, tag.size))
                elif tag.signature == "vcgt":
                    has_vcgt_visual = False

            vcgt_msg = " [vcgt tag present]" if has_vcgt_visual else " [No vcgt]"
            self._update_status("Profile loaded." + vcgt_msg)

    def _perform_load(self, path: Path):
        try:
            self.current_profile_view = load_profile(path)
            self.input_profile = path
            self.input_var.set(str(path))

            # [狀態重置]: 載入新檔時清空任何 pending 的編輯狀態
            self.pending_deletions.clear()
            self.current_vcgt_table = None

            self._refresh_tag_list()
        except ProfileError as e:
            messagebox.showerror("Profile Error", str(e))
            self._update_status("Failed to load profile.")

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
            table = read_vcgt(self.input_profile)
            msg = (f"vcgt Payload Parsed Successfully!\n\n"
                   f"Channels: 3 (RGB)\n"
                   f"Entries per channel: {len(table.red)}\n"
                   f"Entry Size: 16-bit")
            messagebox.showinfo("vcgt Information", msg)
        except ProfileError as e:
            messagebox.showerror("Read Error", str(e))

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
