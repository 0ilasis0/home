import struct
import sys


def remove_icc_tag(input_file, output_file, remove_tag=b"MS00"):

    with open(input_file, "rb") as f:
        data = f.read()

    # ICC header = 128 bytes
    if len(data) < 132:
        raise ValueError("不是有效的 ICC profile：檔案太小")

    # ICC profile size stored at bytes 0..3
    original_size = struct.unpack(">I", data[0:4])[0]

    # Number of tags stored at bytes 128..131
    tag_count = struct.unpack(">I", data[128:132])[0]

    tag_table_end = 132 + tag_count * 12

    if len(data) < tag_table_end:
        raise ValueError("ICC tag table 超出檔案範圍")

    tags = []

    for i in range(tag_count):
        entry = 132 + i * 12

        signature = data[entry:entry + 4]
        offset = struct.unpack(">I", data[entry + 4:entry + 8])[0]
        size = struct.unpack(">I", data[entry + 8:entry + 12])[0]

        tags.append((signature, offset, size))

    # 找出要保留的 tags
    kept_tags = [tag for tag in tags if tag[0] != remove_tag]

    removed = [tag for tag in tags if tag[0] == remove_tag]

    if not removed:
        print(f"找不到 tag: {remove_tag.decode('ascii')}")
        return

    # 建立新的 ICC profile
    new_data = bytearray(data[:128])

    # 新的 tag count
    new_data += struct.pack(">I", len(kept_tags))

    # 預留新的 tag table
    new_data += bytearray(len(kept_tags) * 12)

    # ICC tag data 從 tag table 後面開始
    current_offset = len(new_data)

    # ICC data 必須 4-byte aligned
    def align4(x):
        return (x + 3) & ~3

    new_tag_entries = []

    for signature, offset, size in kept_tags:

        # 對齊
        aligned_offset = align4(current_offset)

        if aligned_offset > current_offset:
            new_data += b"\x00" * (aligned_offset - current_offset)

        current_offset = aligned_offset

        tag_data = data[offset:offset + size]

        new_data += tag_data

        new_tag_entries.append(
            (signature, current_offset, size)
        )

        current_offset += size

    # 寫回新的 tag table
    for i, (signature, offset, size) in enumerate(new_tag_entries):

        entry = 132 + i * 12

        new_data[entry:entry + 4] = signature
        new_data[entry + 4:entry + 8] = struct.pack(">I", offset)
        new_data[entry + 8:entry + 12] = struct.pack(">I", size)

    # 更新 ICC profile size
    new_size = len(new_data)

    new_data[0:4] = struct.pack(">I", new_size)

    with open(output_file, "wb") as f:
        f.write(new_data)

    print("完成")
    print(f"輸入檔案 : {input_file}")
    print(f"輸出檔案 : {output_file}")
    print(f"原始大小 : {original_size} bytes")
    print(f"新的大小 : {new_size} bytes")
    print(f"移除 tag  : {remove_tag.decode('ascii')}")


if __name__ == "__main__":

    input_file = r"C:\Users\user\Desktop\CalibratedDisplayProfile-000.icm"
    output_file = r"C:\Users\user\Desktop\CalibratedDisplayProfile-output.icm"

    remove_icc_tag(
        input_file,
        output_file,
        b"vcgt"
    )