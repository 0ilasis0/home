1. 系統架構與核心運算

此設計用於將兩個 8x8 的帶號整數矩陣進行相乘。

矩陣 A 的資料由陣列左側輸入，每一列對應一個輸入值。

矩陣 B 的資料由陣列上方輸入，每一行對應一個輸入值。

系統採用輸出駐留 (Output-stationary) 架構，每個 PE 負責計算輸出矩陣中對應位置的值，並在本地累積 8 個乘積。

部分和 (Partial sums) 不需要離開或在 PE 之間移動。

運算公式為 C[i][j] 等於 A[i][k] 與 B[k][j] 乘積的總和，其中 k 的範圍為 0 到 7。

A 資料會沿著列向右傳遞，並在每個 PE 延遲一個週期。

B 資料會沿著行向下傳遞，同樣在每個 PE 延遲一個週期。

2. 資料型態與溢位處理

輸入資料為 8 位元帶號數 (8-bit signed)，採用二補數表示，範圍從 -128 到 127。

單一乘積結果為 16 位元帶號數。

累積的資料為 32 位元帶號數，乘積在進行累積前必須先進行符號擴充 (Sign-extended)。

遇到溢位 (Overflow) 時，不具備飽和 (Saturation) 機制，也不具備例外旗標 (Exception flag)。

超出 ACC_WIDTH 的數值將依照二補數算術的規則直接截斷 (Truncated)。

3. 單一處理單元 (PE) 規格

當 reset_n 為低電位時，或是 clear 為高電位時，PE 必須清除 a_out、b_out 以及 acc_out。

當 enable 為高電位時，PE 會暫存並轉發 (forward) a_in 與 b_in。

當 enable 為高電位時，PE 會同時執行帶號數的乘積累加 (MAC) 運算。

在輸出階段，enable 必須降為低電位 (deasserted)，以確保所有 64 個累積結果保持穩定。

4. 控制訊號優先級與時序

第一優先級為 reset_n 等於 0，此時應非同步清除控制器、轉發暫存器以及累加器。

第二優先級為 start 等於 1 且 busy 等於 0，此時系統接受新操作、同步清除所有 PE，並將 busy 設為 1。

第三優先級為運算階段 (Computation phase)，此時 busy 等於 1 且 result_valid 等於 0，運算共持續 22 個週期。

第四優先級為輸出階段 (Output phase)，此時 result_valid 會維持高電位 64 個週期。

在輸出最後一筆資料 (result_last) 後，done 會產生一個週期的脈衝 (pulse)，且 busy 降為 0。

頂層模組只有在 busy 為低電位時，才會接受 start 訊號，若 busy 為高電位則忽略 start。

因為輸出介面沒有準備好 (ready) 訊號，接收端不可發生停滯 (stall)。

5. 資料流與資料傾斜 (Data Skewing)

輸入資料必須進行傾斜 (skewed) 處理，確保相同 k 值的 A 與 B 能在同一週期到達目標 PE。

在週期 t，a_stream 的通道 i 必須驅動數值 A[i][t-i]。

在週期 t，b_stream 的通道 j 必須驅動數值 B[t-j][j]。

若索引超出 0 到 7 的範圍，必須驅動數值為零。

非零資料的輸入波前 (wavefront) 可能出現在週期 0 到週期 14 之間。

在週期 15 到週期 21 之間必須提供零值，以排空 (drain) 管線。

最終有效的乘積會在週期 21 抵達右下角的 PE。

第 (i,j) 個 PE 會在週期 k + i + j 接收到第 k 組配對資料。

6. 資料封裝與輸出順序

輸入匯流排從最低有效位元 (LSB) 開始封裝，並以通道 0 作為起點。

a_stream 的通道 i 對應矩陣 A 的第 i 列，其介面表示範圍為 [8i+7 : 8i]。

b_stream 的通道 j 對應矩陣 B 的第 j 行，其介面表示範圍為 [8j+7 : 8j]。

輸出結果採用列優先 (row-major) 順序。

當 result_valid 為高電位時，result_data 會依序輸出 C[0][0] 到 C[7][7]。

每個週期的輸出元素會伴隨其對應的列索引 (result_row) 與行索引 (result_col)。

在第 64 個結果輸出時 (即 C[7][7])，result_last 會轉為高電位。

當 result_valid 為低電位時，result_data 沒有定義的意義。

7. 參數設定與合成繳交要求

ARRAY_SIZE 預設為 8，且此參數必須為 8。

DATA_WIDTH 預設為 8。

ACC_WIDTH 預設為 32，且必須大於或等於 DATA_WIDTH 的兩倍。

INDEX_WIDTH 預設為 3，提供給 8x8 陣列的 result_row 和 result_col 使用。

最終報告需包含 Verilog 程式碼，以及合成結果，其中需包含面積與時序報告。

必須根據合成結果回答關鍵路徑 (critical path)、資料到達時間 (data arrival time)、寬裕時間 (slack) 以及是否符合時序約束 (timing constraint)。