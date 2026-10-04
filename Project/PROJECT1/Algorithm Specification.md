Triangle Rendering Algorithm
1. 幾何條件

輸入：

$$ P_1=(x_1,y_1) $$ $$ P_2=(x_2,y_2) $$ $$ P_3=(x_3,y_3) $$

固定條件：

$$ x_1=x_3 $$ $$ y_1<y_2<y_3 $$ $$ x_1\neq x_2 $$

座標範圍：

$$ 0\le x,y\le7 $$

因此 triangle 一定具有：

左側垂直邊：\(P_1\rightarrow P_3\)
下方斜邊：\(P_1\rightarrow P_2\)
上方斜邊：\(P_3\rightarrow P_2\)
2. 核心資料表示

我們不儲存每一個 pixel。

而是把 triangle 分解成多條 vertical columns。

每條 column 只需要：

$$ \boxed{(x,\;y_{low},\;y_{up})} $$

代表：

$$ y_{low}\le y\le y_{up} $$

時：

$$ (x,y) $$

屬於 triangle。

因此：

一筆資料 = 一條 x 垂直線上的所有 inside pixels。

這是整個演算法最重要的資料壓縮方式。

3. 一開始直接知道的兩條特殊 Column
Column \(x_1\)

因為：

$$ x_1=x_3 $$

所以垂直邊完全已知：

$$ \boxed{(x_1,y_1,y_3)} $$

不需要任何 E calculation。

Column \(x_2\)

因為 P2 是兩條斜邊共同終點，而且：

$$ y=y_2 $$

是 triangle 的水平中心線。

對：

$$ x=x_2 $$

只有 P2 本身 inside。

因此：

$$ \boxed{(x_2,y_2,y_2)} $$

也不需要 tracing。

4. 判斷 boundary tracing 方向

計算：

$$ dx=x_2-x_1 $$

定義：

$$ s=\operatorname{sign}(dx) $$

因此：

$$ s= \begin{cases} +1,&x_1<x_2\\ -1,&x_1>x_2 \end{cases} $$

dx 是實際幾何位移。

s 只代表 boundary tracing 的 x 方向。

5. 計算兩條 Edge 的 dx / dy
Lower edge：P1 → P2
$$ dx_{12}=x_2-x_1 $$ $$ dy_{12}=y_2-y_1 $$
Upper edge：P3 → P2
$$ dx_{32}=x_2-x_3 $$ $$ dy_{32}=y_2-y_3 $$

因為：

$$ x_3=x_1 $$

所以：

$$ \boxed{dx_{12}=dx_{32}=dx} $$

但：

$$ dy_{12}>0 $$ $$ dy_{32}<0 $$
6. Edge Function

使用：

$$ E_{ab}(x,y) = (x-x_a)(y_b-y_a) - (x_b-x_a)(y-y_a) $$

因此：

Lower
$$ \boxed{ E_{12} = (x-x_1)dy_{12} -dx(y-y_1) } $$
Upper
$$ \boxed{ E_{32} = (x-x_3)dy_{32} -dx(y-y_3) } $$

Inside 判斷統一使用：

$$ \boxed{sE\ge0} $$
7. E 的 Incremental Calculation

這是硬體化的核心。

不需要每次重新計算完整 equation。

x + 1
$$ E(x+1,y)=E(x,y)+dy $$
x - 1
$$ E(x-1,y)=E(x,y)-dy $$

因此依照 s：

$$ \boxed{ E\leftarrow E+s\cdot dy } $$
y + 1
$$ E(x,y+1)=E(x,y)-dx $$
y - 1
$$ E(x,y-1)=E(x,y)+dx $$

所以：

Lower 往 y+1：
$$ \boxed{E\leftarrow E-dx} $$
Upper 往 y-1：
$$ \boxed{E\leftarrow E+dx} $$
8. Lower Boundary Tracing

Lower 使用：

$$ E_{12} $$

目標：

找出每條中間 vertical column 的 ylow。

Start

從 P1 的下一個 x-column 開始：

$$ x=x_1+s $$ $$ y=y_1+1 $$
每一步先檢查 termination
Condition 1：碰到 x2

如果下一個 x：

$$ x+s=x_2 $$

則：

$$ \boxed{STOP} $$

不做：

E calculation
inside/outside test
x2 column tracing

因為 x2 column 已知：

$$ (x_2,y_2,y_2) $$
Condition 2：碰到 y2

如果下一個 y：

$$ y+1=y_2 $$

則：

$$ \boxed{STOP} $$

因為 y2 horizontal line 已知。

沒有 termination 才判斷 E12

計算 / 更新：

$$ E_{12} $$

判斷：

$$ sE_{12}\ge0 $$
Inside

代表目前 (x,y) 在 triangle 內。

因此：

x 方向繼續：
$$ x\leftarrow x+s $$
E：
$$ E_{12}\leftarrow E_{12}+sdy_{12} $$

這代表目前 column 的 ylow 已經確定。

Outside

代表 boundary 要往下一個 y。

所以：

$$ y\leftarrow y+1 $$

並：

$$ E_{12}\leftarrow E_{12}-dx $$
9. Upper Boundary Tracing

Upper 使用：

$$ E_{32} $$

目標：

找出每條中間 vertical column 的 yup。

Start

從 P3 的下一個 x-column：

$$ x=x_3+s $$ $$ y=y_3-1 $$
Termination
碰到 x2

如果：

$$ x+s=x_2 $$

直接：

$$ \boxed{STOP} $$

不做 E calculation / inside test。

碰到 y2

如果：

$$ y-1=y_2 $$

直接：

$$ \boxed{STOP} $$
沒有 termination 才判斷 E32
$$ sE_{32}\ge0 $$
Inside
$$ x\leftarrow x+s $$ $$ E_{32}\leftarrow E_{32}+sdy_{32} $$

因此目前 column 的 yup 可以確定。

Outside
$$ y\leftarrow y-1 $$ $$ E_{32}\leftarrow E_{32}+dx $$

繼續尋找同一 column 的 upper boundary。

10. Boundary Data 的形成

當某個 x-column 的上下 boundary 都找到後，就形成：

$$ \boxed{(x,y_{low},y_{up})} $$

例如：

$$ (1,1,3) $$

代表：

x = 1
y = 1, 2, 3

全部是 inside。

11. 特殊 Column 不需要 tracing

因此最後資料一定包含：

左側：
$$ \boxed{(x_1,y_1,y_3)} $$
右側：
$$ \boxed{(x_2,y_2,y_2)} $$

中間：

$$ \boxed{(x_i,y_{low,i},y_{up,i})} $$

由 Lower / Upper tracing 得到。

12. 為什麼碰到 x2 就可以停止？

這是整個方法最漂亮的地方。

當 tracing 某一 column 時，如果：

$$ x+s=x_2 $$

代表：

下一條 column 就是 P2 的 vertical column。

而我們早就知道：

$$ (x_2,y_2,y_2) $$

所以沒有必要再掃描：

$$ (x_2,y),\quad y\neq y_2 $$

因此：

$$ \boxed{\text{碰到 x2 = current column tracing 結束}} $$

不是需要繼續檢查 x2。

13. Output Phase

現在 boundary data 已經全部建立。

例如：

(x1, y1, y3)
(x1+1, ylow1, yup1)
(x1+2, ylow2, yup2)
...
(x2, y2, y2)

接下來完全不需要：

E12
E32
dx/dy tracing

只需要 output。

Output 順序

題目 sample：

$$ \boxed{y\ ascending\rightarrow x\ ascending} $$

所以：

外層
$$ y=y_1\rightarrow y_3 $$
內層

按照：

$$ x=\min(x_1,x_2)\rightarrow\max(x_1,x_2) $$

逐 column 檢查：

$$ y_{low}\le y\le y_{up} $$

如果成立：

$$ \boxed{output(x,y)} $$
14. 整體流程
                P1 P2 P3
                   │
                   ▼
          Calculate dx, dy12, dy32
                   │
                   ▼
          Determine s = sign(dx)
                   │
          ┌────────┴────────┐
          │                 │
          ▼                 ▼
     Known Column       Known Column
     (x1,y1,y3)         (x2,y2,y2)
          │                 │
          └────────┬────────┘
                   │
          ┌────────┴────────┐
          ▼                 ▼
      Lower tracing      Upper tracing
        using E12          using E32
          │                 │
          └────────┬────────┘
                   ▼
       Generate (x, ylow, yup)
          for each column
                   │
                   ▼
          Boundary data ready
                   │
                   ▼
          y = y1 → y3
                   │
                   ▼
          x = left → right
                   │
                   ▼
        if ylow ≤ y ≤ yup
                   │
                   ▼
             output (x,y)
15. 整套演算法最重要的 6 個重點
① 不逐 pixel 做 point-in-triangle

不是：

每個 (x,y) 都拿去測試 triangle。

而是：

找出每條 vertical column 的 inside range。

② 每筆 boundary data 只有三個值
$$ \boxed{x,\ y_{low},\ y_{up}} $$
③ x1、x2 是特殊 column

直接知道：

$$ (x_1,y_1,y_3) $$

以及：

$$ (x_2,y_2,y_2) $$

不需要 tracing。

④ E12 / E32 只負責 boundary tracing

而且利用：

$$ E(x+1,y)=E+dy $$ $$ E(x-1,y)=E-dy $$ $$ E(x,y+1)=E-dx $$ $$ E(x,y-1)=E+dx $$

避免反覆乘法。

⑤ 碰到 x2 / y2 立即停止

不對該 termination point 做 E calculation 或 inside/outside 判斷。

這是演算法正式定義的一部分。

⑥ Boundary generation 與 output 完全分離

Boundary phase：

$$ \boxed{E12/E32\rightarrow(x,ylow,yup)} $$

Output phase：

$$ \boxed{(x,ylow,yup)\rightarrow(x,y)} $$

這樣既符合 sample 的：

$$ \boxed{y\ down\rightarrow up,\quad x\ left\rightarrow right} $$

也讓後面的硬體架構容易切成兩個清楚的階段。

到這裡，我認為演算法層已經可以定稿；下一步最適合做的是把這套流程用幾個代表性 triangle 手算完整，尤其是 x1<x2、x1>x2、|dx|=1、|dx|=2，確認 (x,ylow,yup) 的產生過程。之後再做真正的 exhaustive verification。