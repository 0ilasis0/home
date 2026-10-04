import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk
from typing import Optional

from .errors import ProfileError
from .models import VcgtTable
from .service import (ProfileView, delete_profile_tags, import_vcgt_txt,
                      load_profile, read_vcgt, save_vcgt)


class IccTagEditorApp:
    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("ICC Profile Tag Editor")
        self.root.geometry("600x500")

        self.input_profile: Optional[Path] = None
        self.output_profile: Optional[Path] = None
        self.current_profile_view: Optional[ProfileView] = None
        self.current_vcgt_table: Optional[VcgtTable] = None

        self._build_ui()

    def _build_ui(self):
        main_frame = ttk.Frame(self.root, padding=10)
        main_frame.pack(fill=tk.BOTH, expand=True)

        # --- Input / Output Section ---
        io_frame = ttk.Frame(main_frame)
        io_frame.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(io_frame, text="Input:").grid(row=0, column=0, sticky=tk.W, pady=2)
        self.input_var = tk.StringVar()
        ttk.Entry(io_frame, textvariable=self.input_var, width=50).grid(row=0, column=1, padx=5, pady=2)
        ttk.Button(io_frame, text="Browse", command=self._browse_input).grid(row=0, column=2, pady=2)

        ttk.Label(io_frame, text="Output:").grid(row=1, column=0, sticky=tk.W, pady=2)
        self.output_var = tk.StringVar()
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

        ttk.Button(action_frame, text="Read vcgt", command=self._action_read_vcgt).grid(row=0, column=0, padx=5)
        ttk.Button(action_frame, text="Import vcgt TXT", command=self._action_import_txt).grid(row=0, column=1, padx=5)
        ttk.Button(action_frame, text="Save vcgt", command=self._action_save_vcgt).grid(row=1, column=0, padx=5, pady=5)
        ttk.Button(action_frame, text="Delete Selected Tags", command=self._action_delete_tags).grid(row=1, column=1, padx=5, pady=5)

        # --- Status Section ---
        self.status_var = tk.StringVar(value="Status: Ready")
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
            for tag in self.current_profile_view.tags:
                self.tree.insert("", tk.END, values=(tag.signature, tag.offset, tag.size))

            vcgt_msg = " [vcgt tag present]" if self.current_profile_view.has_vcgt else " [No vcgt]"
            self._update_status("Profile loaded." + vcgt_msg)

    def _perform_load(self, path: Path):
        try:
            self.current_profile_view = load_profile(path)
            self.input_profile = path
            self.input_var.set(str(path))
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
            messagebox.showinfo("Import Success", "vcgt TXT imported successfully. Ready to save.")
            self._update_status("vcgt TXT imported.")
        except ProfileError as e:
            messagebox.showerror("Import Error", str(e))

    def _action_save_vcgt(self):
        if not self.input_profile:
            messagebox.showwarning("Warning", "Please specify an input profile first.")
            return
        if not self.current_vcgt_table:
            messagebox.showwarning("Warning", "Please import a vcgt TXT first.")
            return

        # [NEW FEATURE] 若未命名 Output，預設填入 output.icc
        out_path_str = self.output_var.get().strip()
        if not out_path_str:
            out_path_str = "output.icc"
            self.output_var.set(out_path_str)

        out_path = Path(out_path_str)
        try:
            save_vcgt(self.input_profile, out_path, self.current_vcgt_table)
            self._update_status("vcgt saved successfully.")
            # Reload output profile to refresh state
            self._perform_load(out_path)
        except ProfileError as e:
            messagebox.showerror("Save Error", str(e))

    def _action_delete_tags(self):
        if not self.input_profile:
            messagebox.showwarning("Warning", "Please specify an input profile first.")
            return

        selected = self.tree.selection()
        if not selected:
            messagebox.showwarning("Warning", "Please select at least one tag to delete.")
            return

        # [NEW FEATURE] 若未命名 Output，預設填入 output.icc
        out_path_str = self.output_var.get().strip()
        if not out_path_str:
            out_path_str = "output.icc"
            self.output_var.set(out_path_str)

        signatures = {str(self.tree.item(item)["values"][0]) for item in selected}
        out_path = Path(out_path_str)

        try:
            delete_profile_tags(self.input_profile, out_path, signatures)
            self._update_status(f"Deleted {len(signatures)} tags.")
            # Reload output profile to refresh state
            self._perform_load(out_path)
        except ProfileError as e:
            messagebox.showerror("Delete Error", str(e))

def main():
    root = tk.Tk()
    app = IccTagEditorApp(root)
    root.mainloop()

if __name__ == "__main__":
    main()