[圖片描述：兩張點陣圖，上方為一朵花，標示座標(x2,y2)；下方為一個橘色幽靈，標示座標(x3,y3)。右方有一個黑色箭頭指向一個64x64大小的綠色背景顯示畫面，畫面中散佈著數個物件，包含花朵、橘色幽靈、彩色糖果與黃色笑臉。]
Digital Circuit Design and Analysis 115-1
Project 2
Triangle Rendering Engine
Acknowledgment: The materials used in this project are adapted from 教育部積體電路設計競賽   
1.問題描述
請完成一Sprite Game Display Engine (後文以SGDE表示)電路設計。如圖1所示,本顯示引擎(Display Engine),接收數個遊戲物件之種類及座標資訊,最後結果輸出在64*64大小的顯示器記憶體。
本控制電路各輸入輸出信號的功能說明,請參考表一。各組同學必須根據第二節所給的設計規格及附錄C中的測試樣本完成設計驗證。
本次專案的作答時間為到11月20日下午5:00。當作答時間結束後,課程助教會根據第三節中的評分標準進行評分。為了評分作業的方便,各組同學應參考附錄E中所列的要求,附上評分所需要的檔案。
本題目完成後請上傳方式另行說明。   
[圖片描述：圖1為系統示意圖。左側有四組座標(x1,y1)到(x4,y4)輸入至中間的「遊戲顯示引擎 (Sprite Game Display Engine)」，右側輸出至一個螢幕顯示畫面。]   
2.設計規格
2.1 系統方塊圖
[圖片描述：圖2為系統方塊圖。左側為Host，輸出clk、reset、sprite、start、type[1:0] (寬度2)、X[5:0] (寬度6)、Y[5:0] (寬度6)至中間的「Sprite Game Display Engine」，並接收來自Engine的ready與done訊號。Engine的右上側連接至「Sprite ROM 38412」，輸出SR_CEN與SR_A[8:0] (寬度9)至ROM的CEN與A[8:0]腳位，ROM的clk則連接系統clk，並輸出SR_Q[12:0] (寬度13)回傳給Engine的SR_Q。Engine的右下側連接至「Frame Buffer 409612」，輸出FB_CEN、FB_WEN、FB_A[11:0] (寬度12)、FB_D[11:0] (寬度12)至Buffer的對應腳位CEN、WEN、A[11:0]、D[11:0]，Buffer的clk連接系統clk，並輸出Q[11:0] (寬度12)回傳給Engine的FB_Q。]   
2.2 輸入/輸出介面
表一-輸入/輸出訊號   
Signal Name   PDFI/O   PDFWidth   PDFSimple Description   PDFclk   
I   
1   
本系統為同步於時脈正緣之同步設計   
reset   
I   
1   
高位準非同步(active high asynchronous)之系統重置信號。   
sprite   
I   
1   
當 HIGH 時表示正輸入一筆物件資料(物件種類type及物件座標X,Y)。   
start   
I   
1   
當 HIGH 時表示所有的物件資料都已經輸入完畢,Game Engine 可以開始產生結果畫面至Frame Buffer中。   
type   
I   
2   
物件種類,2'b00代表Man,2'b01代表Ghost, 2'b10代表 Candy,2'b11代表 Flower   
X   
I   
6   
物件原點(左上角)在畫面上的X座標   
Y   
I   
6   
物件原點(左上角)在畫面上的Y座標   
ready   
Ο   
1   
當HIGH 時代表SGDE可以接受輸入新物件資料   
done   
Ο   
1   
當HIGH 時代表SGDE已經完成動作,已將整個結果畫面產生至Frame Buffer 中   
SR_CEN   
Ο   
1   
Sprite ROM 的Chip Enable 控制訊號,當LOW 時可以讀取 Sprite ROM內部的資料。   
SR_Q   
I   
13   
Sprite ROM 的輸出資料匯流排。   
SR_A   
0   
9   
Sprite ROM 的位址匯流排。   
FB_CEN   
Ο   
1   
Frame Buffer的Chip Enable 控制訊號,當LOW時可以操作 Frame Buffer。   
FB_WEN   
Ο   
1   
Frame Buffer 的Write Enable控制訊號,當LOW時可以寫資料進入Frame Buffer。   
FB_Q   
I   
12   
Frame Buffer 的資料輸出匯流排   
FB_A   
Ο   
12   
Frame Buffer的位址匯流排。   
FB_D   
0   
12   
Frame Buffer 的資料輸入匯流排   
2.3 系統功能描述
SGDE 負責將遊戲畫面寫入顯示器之記憶體中,由主機端(Host)輸入數個遊戲物件的種類及座標,SGDE收集這些資訊將顯示圖形寫入顯示器之記憶體中(Frame Buffer)。每一個遊戲物件都是8*8大小的圖片資料,圖片資料已儲存在Sprite ROM內,如圖3-2所示。SGDE接收到由Host 端輸入的數個遊戲物件座標後,必須從Sprite ROM 取得該遊戲物件之圖片資料,然後根據要求將結果畫出(寫入Frame Buffer)。
輸入的遊戲物件的個數不定,但數目最多不會超過20個遊戲物件。   
2.3.1 輸入資料圖片定義
遊戲物件的圖片資料都儲存在Sprite ROM。每個遊戲物件為-8*8大小的圖片,總共64個pixels。每一個pixel 由13bits 組成,代表RGB 資訊與mask 判斷。圖3-1 為Sprite ROM示意圖,Sprite ROM橫座標位置12~9bit代表R顏色;8~5bits代表G顏色;4~1bits代表B顏色;第0bit 表示此pixel 的mask。
mask 指的是該pixel透明度的設定,當顯示圖片時,僅顯示mask=1的pixel,而mask=0的部份,則顯示底色或其底層的物件顏色,如圖4-1 為Flower 的mask 示意圖,而圖4-2則為Flower 實際顯示圖。關於底色之定義,請參考2.3.2(遊戲畫面底色定義)。   
[圖片描述：圖3-1 Sprite ROM。這是一個陣列示意圖，列位址從0編號到383，橫向標示了13個位元(12到0)。其中位元12到9標示為R，位元8到5標示為G，位元4到1標示為B，位元0標示為mask。]
[圖片描述：圖3-2 Sprite ROM 儲存物件內容。這是一個記憶體區塊配置圖，位址0到63儲存Man (live)，位址64到127儲存Man (happy)，位址128到191儲存Man (die)，位址192到255儲存Ghost，位址256到319儲存Candy，位址320到383儲存Flower。]
[圖片描述：圖4-1 mask 設定。這是一個8x8的網格，裡面填滿了紅色字體的0與1。網格中央由1組成了一個類似花朵與莖葉的形狀對應黑色背景，其餘部分則為0對應白色背景。]
[圖片描述：圖4-2 Flower 實際顯示圖。這是圖4-1中mask為1的部分填上顏色的8x8點陣圖，顯示出一朵黃橘色花瓣、黃色花蕊與綠色莖葉的花朵，mask為0的背景處則呈現淺綠色。]   
遊戲物件總共分為四大類型,分別是 Man(type=0)、Ghost(type=1)、Candy(type=2);與Flower(type=3)。
Man:Man分為三種狀態,分別是live、happy及die,如圖5-1是live狀態的圖形;圖5-2是live 狀態下mask的圖示。圖6-1是happy狀態的圖形;圖6-2是happy狀態下mask 的圖示。圖7-1是die狀態的圖形;圖7-2是die狀態下mask的圖示。Man的狀態受到Man 和其他物件重疊狀況所影響,詳見2.3.3遊戲規則定義。
Ghost: Ghost只有一種狀態,如圖8-1所示;圖8-2是Ghost 狀態下mask的圖示。
Candy: Candy只有一種狀態,如圖9-1所示;圖9-2是Candy 狀態下mask的圖示。
Flower: Flower只有一種狀態,如圖10-1所示;圖10-2是Flower狀態下mask的圖示。   
[圖片描述：圖5-1 live 狀態。為一個黃色圓臉的8x8點陣圖，有深色眼睛與粉色/灰色的嘴部特徵。]
[圖片描述：圖5-2 live 狀態的mask圖示。為圖5-1對應的mask，四個角落共計12個像素為白色(mask=0)，其餘中央呈現圓形的區域為黑色(mask=1)。]
[圖片描述：圖6-1 happy 狀態。為一個笑臉的黃色8x8點陣圖，有黑色眼睛和白色的露齒笑容。]
[圖片描述：圖6-2 happy 狀態的mask圖示。形狀與圖5-2完全相同。]
[圖片描述：圖7-1 die 狀態。為一個藍色的悲傷臉孔8x8點陣圖，有深藍色呈現U字型的眼睛特徵。]
[圖片描述：圖7-1 die 狀態的mask圖示 (註：文件原文標題重複圖號，應為圖7-2)。形狀與圖5-2完全相同。]   
[圖片描述：圖8-1 Ghost 圖示。為一個橘色幽靈形狀的8x8點陣圖，底部有兩隻腳，有兩個深色眼睛。]
[圖片描述：圖8-2 Ghost 狀態的mask圖示。顯示幽靈的輪廓為黑色(mask=1)，其餘背景為白色(mask=0)。]
[圖片描述：圖9-1 Candy 圖示。為一個彩虹色冰淇淋或糖果形狀的8x8點陣圖，上半部為彩色扇形，下半部為紅色棒狀。]
[圖片描述：圖9-2 Candy 狀態的mask圖示。顯示糖果的輪廓為黑色(mask=1)，其餘背景為白色(mask=0)。]
[圖片描述：圖10-1 Flower 圖示。與圖4-2相同的花朵點陣圖。]
[圖片描述：圖10-2 Flower狀態的mask圖示。顯示花朵的輪廓為黑色(mask=1)，背景為白色(mask=0)。]   
每個遊戲物件的圖片資料在Sprite ROM內的儲存位置先後順序如圖11所示,從位置0開始依序儲存到位置63。各物件儲存位置如圖3-2所示。Man(live)的儲存位置在位址為0~63,Man(happy)的儲存位址為64~127,Man(die)的儲存位址為128~191,Ghost的儲存位置在位址為192~255,Candy的儲存位址為256~319,Flower的儲存位址為320~383。   
[圖片描述：圖11物件圖片儲存順序。這是一個8x8的表格，格子內的數字從左上角開始到右下角，由0依序遞增至63。]   
2.3.2 遊戲畫面底色定義
所謂底色表示遊戲畫面最底層的顏色,整個遊戲畫面中,除了遊戲物件 mask=1 的區域以外,其他部分都應填入底色。
畫面的底色定義為12'b 110011110000,即RGB三顏色分別為4b1100,4'b1111與4'60000。   
2.3.3 遊戲規則定義
本遊戲之物件總共分為四大類型:Man、Ghost、Candy 與Flower。其中Man的狀態會受其他物件重疊而有所影響,其規則如下:   
- Man 沒有和其他物件重疊,Man狀態為live。   
- Man 與 Candy 重疊的時候,Man狀態變為happy。   
- Man 與 Ghost重疊的時候,Man狀態變為die。   
- Man 同時與 Candy 和Ghost 重疊的時候,Man狀態變為happy。   
- Man 不管在何種狀態,與Flower 重疊的時候,Man仍維持原來狀態。
- 综合以上的規則,可以定義出如圖12所示的狀態圖。   
[圖片描述：圖12狀態圖。圖中顯示Man的三種狀態節點：live、happy、die。   
- live 節點有指向自身的箭頭，標示 "Man cover Flower" 與 "reset"。   
- live 節點有指向 happy 節點的箭頭，標示 "Man cover Candy"。   
- live 節點有指向 die 節點的箭頭，標示 "Man cover Ghost"。   
- happy 節點有指向自身的箭頭，標示 "Man cover Flower" (從die指向happy的曲線旁也有標示Man cover Candy)。   
- die 節點有指向自身的箭頭，標示 "Man cover Flower"。   
- die 節點有指向 happy 節點的箭頭，標示 "Man cover Candy"。]   
[圖片描述：圖13 Man的mask值。顯示Man的8x8網格，為1的部分以綠色數字顯示於黑色背景，為0的部分以綠色數字顯示於白色背景。]
[圖片描述：圖14 Candy的mask值。顯示Candy的8x8網格，為1的部分以紅色數字顯示於黑色背景，為0的部分以紅色數字顯示於白色背景。]
[圖片描述：圖15 遊戲物件重疊無效判斷。將圖13(Man)與圖14(Candy)以部分重疊的方式交錯放置，但兩者對應mask為1的黑色區塊並沒有在座標上重疊，因此判定為無效。]   
2.3.4圖片資料重疊有效定義
假設兩個8*8圖片資料座標重疊在一起,並非只要重疊在一起就稱為有效。利用Man的mask 圖示與 Candy的mask圖示說明,已經將兩張圖片mask值標上,綠色代表Man的mask值,紅色代表Candy的mask值,如圖13與圖14。
將圖13與圖14的兩張圖片重疊,如圖15所示,很明顯的看到,雖然兩張圖片重疊在一起,但是兩者之間mask值1的部份並沒有重疊,所以判定重疊無效。
利用相同的例子,如圖16所示,調整兩張照片重疊的部份,可以看到兩者之間 mask 值1的部份重疊了,所以判定這兩個物件重疊有效。
Man 和其他物件重疊與否判斷,以Man (live)的mask為判斷依據。   
[圖片描述：圖16遊戲物件重疊有效判斷。調整了Man與Candy圖的相對位置，這次兩者mask為1的黑色區塊有部分交疊在一起，因此判定為重疊有效。]
[圖片描述：圖17圖層重疊顯示圖。展示了一個Man(笑臉)、Candy、Ghost與Flower交疊的渲染結果。Man顯示在最上層，未被任何物件遮擋。Candy與Ghost部分被Man遮擋，而Flower則被Candy和Ghost遮擋，位於最下層。]   
遊戲物件重疊時顯示的順序,Man一定在最上層,Flower一定在下層,至於其他物件圖片重疊顯示的先後順序,依輸入順序決定,先輸入者在下層,後輸入者在上層。如果有多個Flower 物件,先輸入的Flower在下層,後輸入的Flower在上層。而輸入的第一個遊戲物件一定是Man,而且只會有一個Man。例如第一個輸入的遊戲物件是Man,第二個輸入的是Candy,第三個輸入的是Ghost,第四個輸入的是Flower,其重疊顯示由上而下為Man、Ghost、Candy、Flower,如圖17所示。   
再舉一個例子說明,如輸入的順序為Man、Ghost1、Ghost2、Candy1、Flowerl、Ghost3、Flower2、Candy2、Flower3。則重疊時的顯示由上而下為:Man、Candy2、Ghost3、Candy1、Ghost2 Ghost1、Flower3、Flower2、Flowerl。   
2.3.5 顯示器及物件座標定義
SGDE 顯示器為一6464陣列,其原點(0,0)定義於左上角,向右X軸座標增加,向下Y軸座標增加。Host端輸入的物件座標代表該物件左上角對應於顯示器上的座標。
Host 端輸入之物件座標(X,Y)範圍:0≤x≤ 0≤Y≤56,即SGDE不需要考慮物件超出顯示器的狀況。
假設提供一個位於X、Y座標,(10,5)的happy圖片資料,在6464的Sprite Game Display Engine 顯示器中實際的位置如圖18所示。   
[圖片描述：圖18實際座標圖。為一個64x64的網格，左上角標示(0,0)，右上角標示(63,0)，左下角標示(0,63)。在座標(10,5)處繪製了一個佔據8x8方格大小的灰色方塊，標示為happy。]   
2.3.6 顯示器及Frame Buffer 位址定義
Sprite Game Display Engine 顯示器為-64*64陣列,總共有4096個pixel值,如圖19所示,每個pixel 12bits 記錄著 RGB的資訊。顯示器和Frame Buffer的位址對應如圖19和圖 20 所示。圖19的位置0對應到圖20 Frame Buffer位置0的地方,位置1對應到Frame Buffer 位置1的地方,其他位置照此規則。Frame Buffer的每個位址共12bits,代表該pixel的RGB值。位置11~8bit為R顏色;7~4bit為G顏色:3~0bit為B顏色。   
[圖片描述：圖19 4096 pixel 位址。展示了一個64x64的網格，由左至右、由上至下依序填入數字，第一列為0到63，第二列為64到127，依此類推，最後一列為4032到4095。]
[圖片描述：圖20 Frame Buffer。這是一個記憶體結構圖，位址從0編號到4095，圖下方標示每一筆資料為12bits。]   
本顯示引擎的任務是將所有的4096 pixel的RGB資訊儲存在 Frame Buffer中。   
2.4 時序規格
2.4.1 Host 端時序圖   
[時序圖描述：此為Host端時序圖，包含下列訊號線與時序點：   
- clk: 系統時脈，週期標示為 T_CYCLE。   
- reset: 在T1期間發出一個高電位脈衝。   
- ready: 在reset之後拉低。於T2之前拉高，並在sprite輸入後降為低電位。標示了輸出setup time (T_OS)與 hold time (T_OH)。   
- sprite: 在T2期間產生高電位脈衝，標示了輸入setup time (T_IS)與 hold time (T_IH)。   
- X[7:0]、Y[7:0]、type[1:0]: 在T2的sprite脈衝期間，送出有效資料。   
- start: 在T3期間產生一個高電位脈衝。   
- done: 在T4期間產生一個高電位脈衝。]   
1. T1 時間點,系統啟動送出重置(reset)訊號,SGDE進行前置工作準備,準備前置工作期間ready 設為low。   
2. T2時間點,Host發現 ready為high,表示SGDE可以開始接受輸入,host 在clock 負緣送出第一筆物件資料,包含物件種類(type)及物件座標(X,Y),送出物件資料的同時,sprite 升為 high 表示此時物件資料有效。SGDE若無法立刻處理此物件,即將ready設為low。   
3. T2至T3之間,Host以相同方式送出多筆物件資料   
4. T3時間點,物件資料都已送出,此時ready為high,Host送出一個cycle的start 訊號。   
5. SGDE收到start後,進行畫面最後繪製(寫入Frame Buffer),完成後送出done訊號(T4)。   
2.4.2 Host 端時序規格   
Symbol   PDFDescription   PDFValue   PDFT_cycle   
clock period   
user defined   
T_IS   
setup time (time period from valid input signal to positive edge of clock) for all inputs from host.   
1/2 clock period   
T_IH   
hold time(time period from positive edge of clock to invalid input signal.) for all inputs from host   
1/2 clock period   
T_OS   
setup time (from the time at which output is valid to the clock rising edge) for all outputs to the host   
1/4 clock period   
T_OH   
hold time (from the clock rising edge to the time at which output is invalid) for all outputs to the host   
0.1 ns   
2.4.3 Frame Buffer 時序圖
Frame Buffer Read: (FB_CEN=0)
[時序圖描述：此為Frame Buffer Read時序圖，包含：   
- CLK: 系統時脈。   
- FB_WEN: 維持高電位。標示了 setup time (t_WS = 1ns) 與 hold time (t_WH = 0.5ns)。   
- FB_A: 送出Address1, Address2, Address3。標示了 setup time (t_AS = 1ns) 與 hold time (t_AH = 0.5ns)。   
- FB_Q: 在Address1給定後，經過 t_A = 1ns (Access time)，輸出Data1。接著依次輸出Data2, Data3。]   
Frame Buffer Write: (FB_CEN=0)
[時序圖描述：此為Frame Buffer Write時序圖，包含：   
- CLK: 系統時脈。   
- FB_WEN: 產生低電位脈衝以寫入資料。標示了 t_WS = 1ns 與 t_WH = 0.5ns。   
- FB_A: 依次送出Address1, Address2, Address3。標示了 t_AS = 1ns 與 t_AH = 0.5ns。   
- FB_D: 依次寫入Data1, Data2, Data3。標示了 t_DS = 1ns (Data input setup time) 與 t_DH = 0.5ns (Data input hold time)。   
- FB_Q: 在寫入期間，對應位址的輸出訊號Data1, Data2, Data3 (Access time t_A = 1ns)。]   
Symbol   PDFDescription   PDFDelay(ns)   PDFNotes   PDFt_AS   
Address setup time   
1   
Min   
t_AH   
Address hold time   
0.5   
Min   
t_WS   
Write Enable Bar(FB_WEN) setup time   
1   
Min   
t_WH   
Write Enable Bar(FB_WEN) hold time   
0.5   
Min   
t_A   
Access time   
1   
Max   
t_DS   
Data input setup time   
1   
Min   
t_DH   
Data input hold time   
0.5   
Min   
2.4.4 Sprite Rom 時序圖
Sprite ROM Read: (SR_CEN=0)
[時序圖描述：此為Sprite ROM Read時序圖，包含：   
- CLK: 系統時脈。   
- SR_A: 依次送出Address1, Address2, Address3。標示了 t_AS = 1ns 與 t_AH = 0.5ns。   
- SR_Q: 經過 Access time t_A = 1ns 後，依次輸出Data1, Data2, Data3。]   
Symbol   PDFDescription   PDFDelay(ns)   PDFNotes   PDFt_AS   
Address setup time   
1   
Min   
t_AH   
Address hold time   
0.5   
Min   
t_A   
Access time   
1   
Max   
1. 評分標準
2. 課程助教將依照各組同學提供之系統時脈進行RTL simulation、gate-level simulation與 post-layout simulation,以驗證設計正確性。各組同學應於本專案定義的系統時脈下,確保輸出結果無設置與保持時間(setup/hold time)的問題,並完全符合課程助教所提供的標準設計結果為準。
3. 表三為本次專案的評分標準,一旦設計經課程助教驗證後,且同學的設計結果正確(post-layout)將可得到基本分80分,而其次課程助教將依據 Performance Metric的表現,來給予而外的分數,而Performance Metric的計算方式如下所示,其數值越低表示設計的電路效能越好。
- Performance Metric = Time Complexity Area Complexity
1. 
2. Time Complexity:當你完成 Post layout 的 gate level netlist 的ncverilog的驗證時,你可以從“ncverilog.log”的檔案中得到你的Time Complexity結果,如圖十一所示。這個時間代表整個電路完成工作所需的時間,與電路運作頻率(operating frequency)及電路效能(operations per cycle)有關。
3. Area Complexity: Area Complexity可以從SOC encounter 的Layout圖透過尺規量得,如圖21所示。   
[圖片描述：圖21 Layout 尺規量測圖。這是一張SOC Encounter軟體的佈局截圖。畫面左側顯示了電路佈局圖，外部環繞著綠色的Pad ring，內部為密集的紅綠藍佈線(Core區域)。畫面上方標示了紅字「Core size: 445.74um * 442.89um」。畫面右下角有一個放大顯示的工具列截圖，強調點選「Create Ruler (K) (F3 for setting)」，並提示「Please use Create Ruler.」。]   
附錄
在附錄A 說明課程助教提供的遊戲物件的RGB 資訊;附錄B 為課程助教所提供各組同學的設計檔說明;附錄C為課程助教提供的測試樣本;附錄D為設計驗證說明;附錄E為評分用檔案,亦即各組同學必須繳交的檔案資料;附錄F為檔案上傳相關說明。   
附錄A遊戲物件 RGB 資訊
課程助教提供的遊戲物件的RGB資訊紀錄如下(以16進位表示)。   
1. Man_live
2. FFF FFF FE4 FFF FE5 982 FE9 FF5
3. CC3 FE6 FF5 FFO FE6 FF5 FFO FD7
4. FF5 CC3 FFF FE4 982 FFF FFF FE4
5. FF4 FF4 871 FFO FFO 871 FF4 FF4
6. FF3 FF3 FF3 FFO FFO FF3 FF3 FF3
7. FE3 FFO FDB D86 D86 FDB FFO FE3
8. FFF FF0 FFO DCB DCB FF0 FFO FFF
9. FFF FFF EB5 FC6 FC6 DB5 FFF FFF   
10. Man_happy
11. FFF FFF FE9 FE6 FE6 FD7 FFF FFF
12. FFF FE5 FF5 FF5 FF5 FF5 FE4 FFF
13. FE4 FFO 000 FFO FFO 000 FFO FE4
14. FF4 FF0 FF0 FFO FFO FFO FFO FF4
15. FF3 FF3 FF3 FFO FFO FF3 FF3 FF3
16. FE3 FFO FFF FFF FFF FFF FFO FE3
17. FFF FFO FFO FFF FFF FFO FFO FFF
18. FFF FFF EB5 FC6 FC6 DB5 FFF FFF   
19. Man die
20. FFF FFF BDF BDF BDF BDF FFF FFF
21. FFF ACF ACF ACF ACF ACF ACF FFF
22. 9BF 9BF 79C 9BF 9BF 79C 9BF 9BF
23. 6AF 6AF 47A 6AF 6AF 47A 6AF 6AF
24. 6AF 6AF 6AF 6AF 47C 6AF 6AF 6AF
25. 6AF 6AF 47C 6AF 6AF 6AF 6AF 6AF
26. FFF 9DF 7AC 58A 58A 7AC 9DF FFF
27. FFF AEF AEF AEF AEF AEF AEF FFF   
28. Ghost
29. FFF FFF FFF F90 F90 FFF FFF FFF
30. FFF FFF F90 F90 F90 F90 FFF FFF
31. FFF F90 FFF F90 F90 FFF F90 FFF
32. F90 F90 F90 F90 F90 F90 F90 F90
33. F90 F90 F90 F90 F90 F90 F90 F90
34. F90 F90 F90 F90 F90 F90 F90 F90
35. FFF F90 FFF F90 F90 FFF F90 FFF
36. FFF F90 FFF FFF FFF FFF F90 FFF   
37. Candy
38. FFF FFF FFF F43 F86 FFF FFF FFF
39. FFF FFF B12 F80 F80 DFO FFF FFF
40. FFF F90 F61 FCO CEO 3A3 177 FFF
41. FFF FEE EEO FEO 7C2 394 FEE FFF
42. FFF FFF 185 466 11030 100 FFF FFF
43. FFF FFF F77 FAA 629 C8A FFF FFF
44. FFF FFF FFF FOO FOO FFF FFF FFF
45. FFF FFF FFF B12 E12 FFF FFF FFF   
46. Flower
47. FFF FFF FFF FA3 FA3 FFF FFF FFF
48. FFF FFF F90 F50 F50 F90 FFF FFF
49. FFF F50 FAI FF0 FFO F50 FB7 FFF
50. FFF FCB F50 FFO FEO F82 FFF FFF
51. FFF FFF FCB F80 FDD FFF FFF 3B3
52. CEC 8C8 FFF 060 3B3 FFF 060 FFF
53. FFF 3B3 060 050 050 060 FFF FFF
54. FFF FFF FFF 080 060 FFF FFF FFF   
附錄B設計檔(For verilog or VHDL)   
1. 下表為課程助教所提供各組同學的設計檔
2. 表二   
檔名   PDF說明   PDFtestfixture.v   
測試樣本檔。此測試樣本檔定義了時脈週期與測試樣本之輸入信號   
Encounter_example   
SOC Encounter的範例檔(包含mmmc, LEF, GDS 生成指令......)   
SGDE.v   
參賽者所使用的設計檔,已包含系統輸/出入埠之宣告   
FB.v   
Frame Buffer 的verilog 檔   
SR.v   
Sprite ROM的verilog 檔   
SR_verilog.rcf   
Sprite ROM 資料   
synopsys_dc.setup   
Design Compiler 設定檔   
SGDE.tcl   
The script for Design Compiler constraint   
input1.dat   
第一組測試樣本之指令   
golden_image1.xpm   
第一組測試樣本使用之影像檔   
goldenl.dat   
第一組測試樣本之正確結果   
input2.dat   
第二組測試樣本之指令   
golden_image2.xpm   
第二組測試樣本使用之影像檔   
golden2.dat   
第二組測試樣本之正確結果   
input3.dat   
第三組測試樣本之指令   
golden_image3.xpm   
第三組測試樣本使用之影像檔   
golden3.dat   
第三組測試樣本之正確結果   
input4.dat   
第四組測試樣本之指令   
golden_image4.xpm   
第四組測試樣本使用之影像檔   
golden4.dat   
第四組測試樣本之正確結果   
input5.dat   
第五組測試樣本之指令   
golden_image5.xpm   
第五組測試樣本使用之影像檔   
golden5.dat   
第五組測試樣本之正確結果   
1. 請使用SGDE.v,進行遊戲引擎控制器之設計。其模組名稱、輸出/入埠宣告如下所示:   
module SGDE (ready, done, clk, reset, sprite, start, type, X, Y, SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q);[cite: 19]
input clk, reset, sprite, start;[cite: 19]
input [1:0] type;[cite: 19]
input [5:0] X, Y;
input [12:0] SR_Q;
input [11:0] FB_Q;
output ready, done;
output SR_CEN, FB_CEN, FB_WEN;
output [8:0] SR_A;
output [11:0] FB_A, FB_D;
endmodule
1. 此專案共提供五組測試樣本,請自行修改testfixture.v內容,以使用其它組測試樣本,修改方法如下(以第二組為例):   
`define INDATAFILE "./input1.dat"
`define GOLDENDATA "./goldenl.dat"
`define OUTIMAGE "./image1.xpm"
將以上三行修改為:   
`define INDATAFILE "./input2.dat"[cite: 20]
`define GOLDENDATA "./golden2.dat"[cite: 20]
`define OUTIMAGE "./image2.xpm"[cite: 20]
1. 使用verilog 模擬之後,會產生一個影像檔image1.xpm (OUTIMAGE 定義之檔名)。[cite: 20]此影像檔為模擬遊戲顯示器之輸出結果,請執行底下的UNIX指令以觀看此影像:[cite: 20]
xv image1.xpm &[cite: 20]
1. 請各組同學使用課程助教所提供的測試樣本(testfixture.v),來進行設計之模擬驗證。[cite: 20]各組同學除了將利用本試題所提供的測試樣本進行各組同學之設計測試外,我們亦準備另一份測試樣本,進行設計之第二次驗證。[cite: 20]唯通過二次驗證且均能符合輸出結果,始能稱為無誤之設計。[cite: 20]
2. 測試樣本(testfixture.v) 包含 clk, reset, sprite, start, 與 type 測試信號。[cite: 20]而於測試樣本內所定義的時脈週期參數(CYCLE),可由同學們依需求自行修改。[cite: 20]
附錄C測試樣本[cite: 20]
此專案共提供五組測試樣本,為方便設計者除錯之用,課程助教提供的遊戲物件座標以及遊戲物件型式詳列如下:[cite: 20]
Man(type=0) Ghost(type=l) Candy(type=2) Flower(type=3)[cite: 20]
1. 測試樣本一:input1.dat   
順序   PDF1   PDF2   PDF3   PDF4   PDF5   PDF6   PDF7   PDFtype   
0   
1   
2   
1   
3   
2   
3   
X   
23   
8   
30   
53   
53   
35   
8   
Y   
46   
43   
18   
10   
50   
40   
10   
1. 測試樣本二:input2.dat   
順序   PDF1   PDF2   PDF3   PDF4   PDF5   PDF6   PDF7   PDF8   PDFtype   
0   
3   
1   
2   
2   
3   
1   
3   
X   
30   
48   
31   
25   
36   
49   
35   
28   
Y   
30   
53   
23   
26   
28   
49   
35   
33   
1. 測試樣本三:input3.dat   
順序   PDF1   PDF2   PDF3   PDF4   PDF5   PDF6   PDF7   PDF8   PDF9   PDF10   PDFtype   
0   
1   
3   
2   
1   
2   
3   
2   
1   
3   
X   
30   
55   
48   
17   
30   
25   
28   
36   
35   
18   
Y   
30   
8   
53   
20   
23   
27   
33   
28   
35   
43   
1. 測試樣本四:input4.dat   
順序   PDF1   PDF2   PDF3   PDF4   PDF5   PDF6   PDF7   PDF8   PDF9   PDF10   PDFtype   
0   
1   
3   
3   
2   
3   
1   
1   
3   
1   
X   
30   
5   
23   
18   
7   
8   
25   
41   
48   
33   
Y   
30   
8   
12   
43   
20   
13   
5   
23   
53   
34   
順序   PDF11   PDF12   PDF13   PDF14   PDF15   PDF16   PDF17   PDF18   PDF19   PDF20   PDFtype   
2   
2   
3   
1   
1   
2   
2   
1   
3   
1   
X   
21   
25   
28   
5   
31   
47   
54   
5   
9   
21   
Y   
2   
27   
35   
35   
3   
2   
56   
55   
55   
43   
1. 測試樣本五:input5.dat   
順序   PDF1   PDF2   PDF3   PDF4   PDF5   PDF6   PDF7   PDF8   PDF9   PDF10   PDFtype   
0   
3   
1   
2   
2   
3   
2   
1   
1   
3   
X   
30   
48   
55   
17   
25   
13   
36   
35   
23   
18   
Y   
30   
53   
8   
20   
26   
28   
28   
15   
43   
43   
順序   PDF11   PDF12   PDF13   PDF14   PDF15   PDF16   PDF17   PDF18   PDF19   PDF20   PDFtype   
3   
1   
3   
2   
3   
2   
1   
1   
3   
1   
X   
48   
55   
17   
15   
28   
47   
35   
5   
47   
20   
Y   
3   
4   
2   
26   
33   
27   
35   
54   
20   
26   
[圖片描述：附錄D中提供之Verify Connectivity軟體操作截圖。左側為設定視窗，右側為執行後的報告(PUB.conn.rpt)，紅色框線強調報告結尾處顯示「Found no problems or warnings.」。]
[圖片描述：附錄D中提供之Verify Geometry軟體操作截圖。左側為設定視窗，右側為執行後的報告(PUB.geom.rpt)，紅色框線強調報告結尾處顯示「No DRC violation were found」。]   
附錄D設計驗證說明
各組同學繳交資料前應完成RTL,Gate-Level 與Physical 三種階段驗證,以確保設計正確性。   
- RTL 與Gate-Level階段:同學們必須進行RTL simulation 及Gate-Level simulation,模擬結果必須於參賽者自行定義的系統時脈下,輸出結果正確且無setup/hold time 的問題。   
- Physical 階段,包含三項驗證重點:
- 1.完成最後layout
- i. Marco layout,不含IO Pad。
- ii. VCC 與GND power ring 請各設定為2um。
- 2.完成post-layout simulation:參賽者必須使用P&R軟體寫出之 netlist 檔與sdf檔完成 post-layout gate-level simulation
- 3.完成DRC 與LVS驗證:參賽者必須以其所使用之P&R軟體內含之DRC 與LVS驗證功能完成DRC與LVS驗證
- i 驗證 DRC 與LVS步驟如下: 在SOC Encounter 視窗下點選"Verify →Verify Connectivity... "Default值,按OK。   
在此步驟完成後會產生*.conn.rpt檔,如驗證結果正確,會顯示如右圖紅框處之文字。
"Verify → Verify Geometry... " Default值,按OK。
在此步驟完成後會產生*.geom.rpt檔,如驗證結果正確,會顯示如右圖紅框處之文字。   
附錄E評分用檔案
評分所須檔案可以分為以下幾個部份:(1)RTL design,即各組同學對該次專案設計的RTL code,若設計採模組化而有多個設計檔,請務必將合成所要用到的各module檔放進來,以免助教進行評分時,無法進行模擬;(2)Gate-Level design,即由合成軟體所產生的 gate-level netlist,以及對應的SDF檔;(3)Physical design,即由Cadence SOC Encounter所產生的GDS Layout 檔,以及DRC/LVS report 檔等。
表三   
Design Stage   PDFFile   PDFDescription   PDFRTL category
   
N/A   
*.doc   
Design Report Form   
RTL Simulation   
*.v or *.vhd   
Verilog (or VHDL) synthesizable RTL code   
Gate-Level category
   
Pre-layout Gate-level   
*_syn.v

 *_syn.sdf   
Verilog gate-level netlist generated by Synopsys Design Compiler

 Pre-layout gate-level sdf   
Simulation   
*_syn.vcd   
Wave information generated by Ncverilog   
ncverilog.log   
The log file generated by Ncverilog   
Physical category
   
P&R   
*.gds   
GDSII layout   
*.conn.rpt

 *.geom.rpt   
DRC/LVS report generated by Cadence SOC Encounter   
Post-layout   
*_pr.v   
Verilog gate-level netlist generated by Cadence SOC Encounter   
Gate-level   
*_pr.sdf   
Post-layout gate-level sdf   
Simulation   
*_pr.vcd   
Wave information generated by Ncverilog   
ncverilog.log   
The log file generated by Ncverilog   

1. 各物件精確的 8×8 Mask 矩陣（極重要！）原文件中僅給出 Flower 的 0/1 數字矩陣，其餘物件（Man、Ghost、Candy）在 PDF 中僅為黑白方格圖。若要在硬體中預先判斷 Man 的重疊狀態（或是比對 SR_Q[0]），其他 AI 必須知道精確的 8×8 Mask 分佈（1 為顯示/不透明，0 為透明）：   Man (live / happy / die 三種狀態的 Mask 皆相同)：   PlaintextRow 0: 0 0 1 1 1 1 0 0
Row 1: 0 1 1 1 1 1 1 0
Row 2: 1 1 1 1 1 1 1 1
Row 3: 1 1 1 1 1 1 1 1
Row 4: 1 1 1 1 1 1 1 1
Row 5: 1 1 1 1 1 1 1 1
Row 6: 0 1 1 1 1 1 1 0
Row 7: 0 0 1 1 1 1 0 0
Ghost (type = 2'b01)：   PlaintextRow 0: 0 0 0 1 1 0 0 0
Row 1: 0 0 1 1 1 1 0 0
Row 2: 0 1 1 1 1 1 1 0
Row 3: 1 1 1 1 1 1 1 1
Row 4: 1 1 1 1 1 1 1 1
Row 5: 1 1 1 1 1 1 1 1
Row 6: 0 1 0 1 1 0 1 0
Row 7: 0 1 0 0 0 0 1 0
Candy (type = 2'b10)：   PlaintextRow 0: 0 0 0 1 1 0 0 0
Row 1: 0 0 1 1 1 1 0 0
Row 2: 0 1 1 1 1 1 1 0
Row 3: 0 1 1 1 1 1 1 0
Row 4: 0 0 1 1 1 1 0 0
Row 5: 0 0 1 1 1 1 0 0
Row 6: 0 0 0 1 1 0 0 0
Row 7: 0 0 0 1 1 0 0 0
Flower (type = 2'b11)：   PlaintextRow 0: 0 0 0 1 1 0 0 0
Row 1: 0 0 1 1 1 1 0 0
Row 2: 0 1 1 1 1 1 1 0
Row 3: 0 1 1 1 1 1 0 0
Row 4: 0 0 1 1 1 0 0 1
Row 5: 1 1 0 1 1 0 1 0
Row 6: 0 1 1 1 1 1 0 0
Row 7: 0 0 0 1 1 0 0 0
(註：上述 Mask 亦可於執行期間直接從 Sprite ROM 讀出的 SR_Q[0] 取得。)   2. testfixture.v 的實際交握（Handshake）與驗證行為規格書文字未完全寫明 Testbench 的精確觸發邊緣，設計 RTL 時必須嚴格遵守以下 testfixture.v 行為：Reset 行為：模擬開始時，clk 初始為 0，經過 1 個 Clock 週期後 reset 拉高為 1 維持 1 個 Clock 週期，接著拉低為 0。Host 輸入時機（Negative Edge 觸發）：Testbench 在 posedge clk 檢查 if (ready == 1)。若 ready == 1，Testbench 會等到緊接著的 negedge clk 才將 sprite 拉為 1，並同時放上 X、Y、type。   如果你的設計在每個 cycle 都能連續收資料（建議做法：直接用長度為 20 的 Register Array 存下所有輸入的 X, Y, type，讓 ready 在收資料期間保持為 1），Testbench 就會每個 cycle 連續送入一筆物件，直到全數送完（最多 20 筆）。   當最後一筆物件送完的下一個 posedge clk（若 ready == 1），Testbench 會在緊接著的 negedge clk 將 sprite 拉為 0、並將 start 拉為 1（維持一個週期至下一個 negedge clk 變回 0）。   Done 與記憶體釋放：當 SGDE 拉起 done = 1 的瞬間（posedge done），Testbench 會在下一個 negedge clk 將 fb_ctrl 設為 1，強行接管 Frame Buffer 的 CEN、WEN 與 A 來逐一檢查答案。因此在拉起 done 之前，4096 個像素必須已經全部寫入完畢。   3. 記憶體（SR 與 FB）的精確讀寫延遲（Latency）同步觸發：SR (Sprite ROM) 與 FB (Frame Buffer) 都是同步於 posedge clk 的單埠記憶體。   讀取延遲：當在 Cycle $N$ 的 posedge clk 時滿足 SR_CEN == 0 且給定 SR_A，資料 SR_Q 會在 posedge clk 之後經過 $t_A = 1\text{ns}$ 輸出，也就是在 Cycle $N+1$ 的 posedge clk 才能被 SGDE 的暫存器安全鎖存（Latch）。   寫入行為：當在 Cycle $N$ 的 posedge clk 時滿足 FB_CEN == 0 且 FB_WEN == 0，位址 FB_A 與資料 FB_D 會在該正緣直接寫入 FB 記憶體中。   4. 座標與位址換算公式Frame Buffer 位址 (FB_A)：64×64 畫面中，像素座標 $(x, y)$（其中 $0 \le x \le 63, 0 \le y \le 63$）對應的 12-bit 位址為：
   $$\text{FB\_A} = (y \times 64) + x = \{y[5:0], x[5:0]\}$$Sprite ROM 位址 (SR_A)：物件內部相對座標 $(dx, dy)$（其中 $0 \le dx \le 7, 0 \le dy \le 7$）對應的 9-bit 位址為：
   $$\text{SR\_A} = (\text{Base\_Index} \times 64) + (dy \times 8) + dx = \{\text{Base\_Index}[2:0], dy[2:0], dx[2:0]\}$$
其中 Base_Index 對應關係為：   Man (live)：3'd0 (0~63)   Man (happy)：3'd1 (64~127)   Man (die)：3'd2 (128~191)   Ghost：3'd3 (192~255)   Candy：3'd4 (256~319)   Flower：3'd5 (320~383)   5. 文本中的 OCR 勘誤修正你貼出的文本中有幾處因 PDF 轉文字產生的誤植，必須明確提醒其他 AI 以免寫錯數值：2.1 節系統方塊圖描述：「Sprite ROM 38412」與「Frame Buffer 409612」實為 384×13（圖上寫 384*12 但資料匯流排為 13 bits SR_Q[12:0]）與 4096×12。   2.3.2 節底色定義：「4b1100, 4'b1111與4'60000」中的 4'60000 是 OCR 誤植，正確為 4'b0000，即底色固定為 12'b1100_1111_0000 (12'hCF0)。   2.3.5 節座標範圍：「0≤x≤ 0≤Y≤56」漏了 56，正確為 $0 \le X \le 56$ 且 $0 \le Y \le 56$。   2.4.1 節時序圖訊號寬度：時序圖上標示 X[7:0]、Y[7:0] 為圖面筆誤，實際硬體 Port 寬度以 SGDE.v 宣告為準，為 X[5:0]、Y[5:0] (6 bits)。   附錄 A 的 RGB 表格誤植（僅供參考，實際運作以 SR_verilog.rcf 檔案內存值為準）：   Man_live 前三行在轉文字時欄位順序被打亂。   Ghost 第 4 行（Row 3）漏了兩個眼睛的黑色像素 444 或 544（對應 testfixture.v 色表中的 #504A41，即 12'b0101_0100_0100 = 12'h544）。   Candy 第 5 行（Row 4）中的 11030 實為 03D 與 00E 兩個像素黏在一起的 OCR 錯誤。   由於模擬時 SR.v 會自動載入 SR_verilog.rcf，請其他 AI 直接透過 SR_A 讀取 SR_Q 即可，切勿將附錄 A 的 Hex 值硬編碼（Hardcode）在電路中。