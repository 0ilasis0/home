[圖片描述：兩張點陣圖，上方為一朵花，標示座標(x2,y2)；下方為一個橘色幽靈，標示座標(x3,y3)。[cite: 1]右方有一個黑色箭頭指向一個64x64大小的綠色背景顯示畫面，畫面中散佈著數個物件，包含花朵、橘色幽靈、彩色糖果與黃色笑臉。[cite: 1]]
Digital Circuit Design and Analysis 115-1[cite: 1]
Project 2[cite: 1]
Triangle Rendering Engine[cite: 1]
Acknowledgment: The materials used in this project are adapted from 教育部積體電路設計競賽[cite: 1]

1.問題描述[cite: 1]
請完成一Sprite Game Display Engine (後文以SGDE表示)電路設計。[cite: 1]如圖1所示,本顯示引擎(Display Engine),接收數個遊戲物件之種類及座標資訊,最後結果輸出在64*64大小的顯示器記憶體。[cite: 1]
本控制電路各輸入輸出信號的功能說明,請參考表一。[cite: 1]各組同學必須根據第二節所給的設計規格及附錄C中的測試樣本完成設計驗證。[cite: 1]
本次專案的作答時間為到11月20日下午5:00。[cite: 1]當作答時間結束後,課程助教會根據第三節中的評分標準進行評分。[cite: 1]為了評分作業的方便,各組同學應參考附錄E中所列的要求,附上評分所需要的檔案。[cite: 1]
本題目完成後請上傳方式另行說明。[cite: 1]

[圖片描述：圖1為系統示意圖。左側有四組座標(x1,y1)到(x4,y4)輸入至中間的「遊戲顯示引擎 (Sprite Game Display Engine)」，右側輸出至一個螢幕顯示畫面。[cite: 1]]

2.設計規格[cite: 2]
2.1 系統方塊圖[cite: 2]
[圖片描述：圖2為系統方塊圖。左側為Host，輸出clk、reset、sprite、start、type[1:0] (寬度2)、X[5:0] (寬度6)、Y[5:0] (寬度6)至中間的「Sprite Game Display Engine」，並接收來自Engine的ready與done訊號。[cite: 2]Engine的右上側連接至「Sprite ROM 384*12」，輸出SR_CEN與SR_A[8:0] (寬度9)至ROM的CEN與A[8:0]腳位，ROM的clk則連接系統clk，並輸出SR_Q[12:0] (寬度13)回傳給Engine的SR_Q。[cite: 2]Engine的右下側連接至「Frame Buffer 4096*12」，輸出FB_CEN、FB_WEN、FB_A[11:0] (寬度12)、FB_D[11:0] (寬度12)至Buffer的對應腳位CEN、WEN、A[11:0]、D[11:0]，Buffer的clk連接系統clk，並輸出Q[11:0] (寬度12)回傳給Engine的FB_Q。[cite: 2]]

2.2 輸入/輸出介面[cite: 2]
表一-輸入/輸出訊號[cite: 2]

| Signal Name[cite: 2] | I/O[cite: 2] | Width[cite: 2] | Simple Description[cite: 2] |
| :--- | :--- | :--- | :--- |
| clk[cite: 2] | I[cite: 2] | 1[cite: 2] | 本系統為同步於時脈正緣之同步設計[cite: 2] |
| reset[cite: 2] | I[cite: 2] | 1[cite: 2] | 高位準非同步(active high asynchronous)之系統重置信號。[cite: 2] |
| sprite[cite: 2] | I[cite: 2] | 1[cite: 2] | 當 HIGH 時表示正輸入一筆物件資料(物件種類type及物件座標X,Y)。[cite: 2] |
| start[cite: 2] | I[cite: 2] | 1[cite: 2] | 當 HIGH 時表示所有的物件資料都已經輸入完畢,Game Engine 可以開始產生結果畫面至Frame Buffer中。[cite: 2] |
| type[cite: 2] | I[cite: 2] | 2[cite: 2] | 物件種類,2'b00代表Man,2'b01代表Ghost, 2'b10代表 Candy,2'b11代表 Flower[cite: 2] |
| X[cite: 2] | I[cite: 2] | 6[cite: 2] | 物件原點(左上角)在畫面上的X座標[cite: 2] |
| Y[cite: 2] | I[cite: 2] | 6[cite: 2] | 物件原點(左上角)在畫面上的Y座標[cite: 2] |
| ready[cite: 2] | Ο[cite: 2] | 1[cite: 2] | 當HIGH 時代表SGDE可以接受輸入新物件資料[cite: 2] |
| done[cite: 2] | Ο[cite: 2] | 1[cite: 2] | 當HIGH 時代表SGDE已經完成動作,已將整個結果畫面產生至Frame Buffer 中[cite: 2][cite: 3] |
| SR_CEN[cite: 3] | Ο[cite: 3] | 1[cite: 3] | Sprite ROM 的Chip Enable 控制訊號,當LOW 時可以讀取 Sprite ROM內部的資料。[cite: 3] |
| SR_Q[cite: 3] | I[cite: 3] | 13[cite: 3] | Sprite ROM 的輸出資料匯流排。[cite: 3] |
| SR_A[cite: 3] | 0[cite: 3] | 9[cite: 3] | Sprite ROM 的位址匯流排。[cite: 3] |
| FB_CEN[cite: 3] | Ο[cite: 3] | 1[cite: 3] | Frame Buffer的Chip Enable 控制訊號,當LOW時可以操作 Frame Buffer。[cite: 3] |
| FB_WEN[cite: 3] | Ο[cite: 3] | 1[cite: 3] | Frame Buffer 的Write Enable控制訊號,當LOW時可以寫資料進入Frame Buffer。[cite: 3] |
| FB_Q[cite: 3] | I[cite: 3] | 12[cite: 3] | Frame Buffer 的資料輸出匯流排[cite: 3] |
| FB_A[cite: 3] | Ο[cite: 3] | 12[cite: 3] | Frame Buffer的位址匯流排。[cite: 3] |
| FB_D[cite: 3] | 0[cite: 3] | 12[cite: 3] | Frame Buffer 的資料輸入匯流排[cite: 3] |

2.3 系統功能描述[cite: 3]
SGDE 負責將遊戲畫面寫入顯示器之記憶體中,由主機端(Host)輸入數個遊戲物件的種類及座標,SGDE收集這些資訊將顯示圖形寫入顯示器之記憶體中(Frame Buffer)。[cite: 3]每一個遊戲物件都是8*8大小的圖片資料,圖片資料已儲存在Sprite ROM內,如圖3-2所示。[cite: 3]SGDE接收到由Host 端輸入的數個遊戲物件座標後,必須從Sprite ROM 取得該遊戲物件之圖片資料,然後根據要求將結果畫出(寫入Frame Buffer)。[cite: 3]
輸入的遊戲物件的個數不定,但數目最多不會超過20個遊戲物件。[cite: 3]

2.3.1 輸入資料圖片定義[cite: 3]
遊戲物件的圖片資料都儲存在Sprite ROM。[cite: 3]每個遊戲物件為-8*8大小的圖片,總共64個pixels。[cite: 3]每一個pixel 由13bits 組成,代表RGB 資訊與mask 判斷。[cite: 3]圖3-1 為Sprite ROM示意圖,Sprite ROM橫座標位置12~9bit代表R顏色;8~5bits代表G顏色;4~1bits代表B顏色;第0bit 表示此pixel 的mask。[cite: 3]
mask 指的是該pixel透明度的設定,當顯示圖片時,僅顯示mask=1的pixel,而mask=0的部份,則顯示底色或其底層的物件顏色,如圖4-1 為Flower 的mask 示意圖,而圖4-2則為Flower 實際顯示圖。[cite: 3]關於底色之定義,請參考2.3.2(遊戲畫面底色定義)。[cite: 3]

[圖片描述：圖3-1 Sprite ROM。這是一個陣列示意圖，列位址從0編號到383，橫向標示了13個位元(12到0)。[cite: 4]其中位元12到9標示為R，位元8到5標示為G，位元4到1標示為B，位元0標示為mask。[cite: 4]]
[圖片描述：圖3-2 Sprite ROM 儲存物件內容。這是一個記憶體區塊配置圖，位址0到63儲存Man (live)，位址64到127儲存Man (happy)，位址128到191儲存Man (die)，位址192到255儲存Ghost，位址256到319儲存Candy，位址320到383儲存Flower。[cite: 4]]
[圖片描述：圖4-1 mask 設定。這是一個8x8的網格，裡面填滿了紅色字體的0與1。[cite: 4]網格中央由1組成了一個類似花朵與莖葉的形狀對應黑色背景，其餘部分則為0對應白色背景。[cite: 4]]
[圖片描述：圖4-2 Flower 實際顯示圖。這是圖4-1中mask為1的部分填上顏色的8x8點陣圖，顯示出一朵黃橘色花瓣、黃色花蕊與綠色莖葉的花朵，mask為0的背景處則呈現淺綠色。[cite: 4]]

遊戲物件總共分為四大類型,分別是 Man(type=0)、Ghost(type=1)、Candy(type=2);與Flower(type=3)。[cite: 4]
Man:Man分為三種狀態,分別是live、happy及die,如圖5-1是live狀態的圖形;圖5-2是live 狀態下mask的圖示。[cite: 4]圖6-1是happy狀態的圖形;圖6-2是happy狀態下mask 的圖示。[cite: 4]圖7-1是die狀態的圖形;圖7-2是die狀態下mask的圖示。[cite: 4]Man的狀態受到Man 和其他物件重疊狀況所影響,詳見2.3.3遊戲規則定義。[cite: 4]
Ghost: Ghost只有一種狀態,如圖8-1所示;圖8-2是Ghost 狀態下mask的圖示。[cite: 4]
Candy: Candy只有一種狀態,如圖9-1所示;圖9-2是Candy 狀態下mask的圖示。[cite: 4]
Flower: Flower只有一種狀態,如圖10-1所示;圖10-2是Flower狀態下mask的圖示。[cite: 4]

[圖片描述：圖5-1 live 狀態。為一個黃色圓臉的8x8點陣圖，有深色眼睛與粉色/灰色的嘴部特徵。[cite: 5]]
[圖片描述：圖5-2 live 狀態的mask圖示。為圖5-1對應的mask，四個角落共計12個像素為白色(mask=0)，其餘中央呈現圓形的區域為黑色(mask=1)。[cite: 5]]
[圖片描述：圖6-1 happy 狀態。為一個笑臉的黃色8x8點陣圖，有黑色眼睛和白色的露齒笑容。[cite: 5]]
[圖片描述：圖6-2 happy 狀態的mask圖示。形狀與圖5-2完全相同。[cite: 5]]
[圖片描述：圖7-1 die 狀態。為一個藍色的悲傷臉孔8x8點陣圖，有深藍色呈現U字型的眼睛特徵。[cite: 5]]
[圖片描述：圖7-1 die 狀態的mask圖示 (註：文件原文標題重複圖號，應為圖7-2)。形狀與圖5-2完全相同。[cite: 5]]

[圖片描述：圖8-1 Ghost 圖示。為一個橘色幽靈形狀的8x8點陣圖，底部有兩隻腳，有兩個深色眼睛。[cite: 6]]
[圖片描述：圖8-2 Ghost 狀態的mask圖示。顯示幽靈的輪廓為黑色(mask=1)，其餘背景為白色(mask=0)。[cite: 6]]
[圖片描述：圖9-1 Candy 圖示。為一個彩虹色冰淇淋或糖果形狀的8x8點陣圖，上半部為彩色扇形，下半部為紅色棒狀。[cite: 6]]
[圖片描述：圖9-2 Candy 狀態的mask圖示。顯示糖果的輪廓為黑色(mask=1)，其餘背景為白色(mask=0)。[cite: 6]]
[圖片描述：圖10-1 Flower 圖示。與圖4-2相同的花朵點陣圖。[cite: 6]]
[圖片描述：圖10-2 Flower狀態的mask圖示。顯示花朵的輪廓為黑色(mask=1)，背景為白色(mask=0)。[cite: 6]]

每個遊戲物件的圖片資料在Sprite ROM內的儲存位置先後順序如圖11所示,從位置0開始依序儲存到位置63。[cite: 6]各物件儲存位置如圖3-2所示。[cite: 6]Man(live)的儲存位置在位址為0~63,Man(happy)的儲存位址為64~127,Man(die)的儲存位址為128~191,Ghost的儲存位置在位址為192~255,Candy的儲存位址為256~319,Flower的儲存位址為320~383。[cite: 6]

[圖片描述：圖11物件圖片儲存順序。這是一個8x8的表格，格子內的數字從左上角開始到右下角，由0依序遞增至63。[cite: 7]]

2.3.2 遊戲畫面底色定義[cite: 7]
所謂底色表示遊戲畫面最底層的顏色,整個遊戲畫面中,除了遊戲物件 mask=1 的區域以外,其他部分都應填入底色。[cite: 7]
畫面的底色定義為12'b 110011110000,即RGB三顏色分別為4b1100,4'b1111與4'60000。[cite: 7]

2.3.3 遊戲規則定義[cite: 7]
本遊戲之物件總共分為四大類型:Man、Ghost、Candy 與Flower。[cite: 7]其中Man的狀態會受其他物件重疊而有所影響,其規則如下:[cite: 7]
* Man 沒有和其他物件重疊,Man狀態為live。[cite: 7]
* Man 與 Candy 重疊的時候,Man狀態變為happy。[cite: 7]
* Man 與 Ghost重疊的時候,Man狀態變為die。[cite: 7]
* Man 同時與 Candy 和Ghost 重疊的時候,Man狀態變為happy。[cite: 7]
* Man 不管在何種狀態,與Flower 重疊的時候,Man仍維持原來狀態。[cite: 7]
综合以上的規則,可以定義出如圖12所示的狀態圖。[cite: 7]

[圖片描述：圖12狀態圖。圖中顯示Man的三種狀態節點：live、happy、die。[cite: 8]
- live 節點有指向自身的箭頭，標示 "Man cover Flower" 與 "reset"。[cite: 8]
- live 節點有指向 happy 節點的箭頭，標示 "Man cover Candy"。[cite: 8]
- live 節點有指向 die 節點的箭頭，標示 "Man cover Ghost"。[cite: 8]
- happy 節點有指向自身的箭頭，標示 "Man cover Flower" (從die指向happy的曲線旁也有標示Man cover Candy)。[cite: 8]
- die 節點有指向自身的箭頭，標示 "Man cover Flower"。[cite: 8]
- die 節點有指向 happy 節點的箭頭，標示 "Man cover Candy"。[cite: 8]]

[圖片描述：圖13 Man的mask值。顯示Man的8x8網格，為1的部分以綠色數字顯示於黑色背景，為0的部分以綠色數字顯示於白色背景。[cite: 9]]
[圖片描述：圖14 Candy的mask值。顯示Candy的8x8網格，為1的部分以紅色數字顯示於黑色背景，為0的部分以紅色數字顯示於白色背景。[cite: 9]]
[圖片描述：圖15 遊戲物件重疊無效判斷。將圖13(Man)與圖14(Candy)以部分重疊的方式交錯放置，但兩者對應mask為1的黑色區塊並沒有在座標上重疊，因此判定為無效。[cite: 9]]

2.3.4圖片資料重疊有效定義[cite: 9]
假設兩個8*8圖片資料座標重疊在一起,並非只要重疊在一起就稱為有效。[cite: 9]利用Man的mask 圖示與 Candy的mask圖示說明,已經將兩張圖片mask值標上,綠色代表Man的mask值,紅色代表Candy的mask值,如圖13與圖14。[cite: 9]
將圖13與圖14的兩張圖片重疊,如圖15所示,很明顯的看到,雖然兩張圖片重疊在一起,但是兩者之間mask值1的部份並沒有重疊,所以判定重疊無效。[cite: 9]
利用相同的例子,如圖16所示,調整兩張照片重疊的部份,可以看到兩者之間 mask 值1的部份重疊了,所以判定這兩個物件重疊有效。[cite: 9]
Man 和其他物件重疊與否判斷,以Man (live)的mask為判斷依據。[cite: 9]

[圖片描述：圖16遊戲物件重疊有效判斷。調整了Man與Candy圖的相對位置，這次兩者mask為1的黑色區塊有部分交疊在一起，因此判定為重疊有效。[cite: 10]]
[圖片描述：圖17圖層重疊顯示圖。展示了一個Man(笑臉)、Candy、Ghost與Flower交疊的渲染結果。[cite: 10]Man顯示在最上層，未被任何物件遮擋。[cite: 10]Candy與Ghost部分被Man遮擋，而Flower則被Candy和Ghost遮擋，位於最下層。[cite: 10]]

遊戲物件重疊時顯示的順序,Man一定在最上層,Flower一定在下層,至於其他物件圖片重疊顯示的先後順序,依輸入順序決定,先輸入者在下層,後輸入者在上層。[cite: 10]如果有多個Flower 物件,先輸入的Flower在下層,後輸入的Flower在上層。[cite: 10]而輸入的第一個遊戲物件一定是Man,而且只會有一個Man。[cite: 10]例如第一個輸入的遊戲物件是Man,第二個輸入的是Candy,第三個輸入的是Ghost,第四個輸入的是Flower,其重疊顯示由上而下為Man、Ghost、Candy、Flower,如圖17所示。[cite: 10]

再舉一個例子說明,如輸入的順序為Man、Ghost1、Ghost2、Candy1、Flowerl、Ghost3、Flower2、Candy2、Flower3。[cite: 11]則重疊時的顯示由上而下為:Man、Candy2、Ghost3、Candy1、Ghost2 Ghost1、Flower3、Flower2、Flowerl。[cite: 11]

2.3.5 顯示器及物件座標定義[cite: 11]
SGDE 顯示器為一64*64陣列,其原點(0,0)定義於左上角,向右X軸座標增加,向下Y軸座標增加。[cite: 11]Host端輸入的物件座標代表該物件左上角對應於顯示器上的座標。[cite: 11]
Host 端輸入之物件座標(X,Y)範圍:0≤x≤ 0≤Y≤56,即SGDE不需要考慮物件超出顯示器的狀況。[cite: 11]
假設提供一個位於X、Y座標,(10,5)的happy圖片資料,在64*64的Sprite Game Display Engine 顯示器中實際的位置如圖18所示。[cite: 11]

[圖片描述：圖18實際座標圖。為一個64x64的網格，左上角標示(0,0)，右上角標示(63,0)，左下角標示(0,63)。[cite: 11]在座標(10,5)處繪製了一個佔據8x8方格大小的灰色方塊，標示為happy。[cite: 11]]

2.3.6 顯示器及Frame Buffer 位址定義[cite: 11]
Sprite Game Display Engine 顯示器為-64*64陣列,總共有4096個pixel值,如圖19所示,每個pixel 12bits 記錄著 RGB的資訊。[cite: 11]顯示器和Frame Buffer的位址對應如圖19和圖 20 所示。[cite: 11]圖19的位置0對應到圖20 Frame Buffer位置0的地方,位置1對應到Frame Buffer 位置1的地方,其他位置照此規則。[cite: 11]Frame Buffer的每個位址共12bits,代表該pixel的RGB值。[cite: 11]位置11~8bit為R顏色;7~4bit為G顏色:3~0bit為B顏色。[cite: 11]

[圖片描述：圖19 4096 pixel 位址。展示了一個64x64的網格，由左至右、由上至下依序填入數字，第一列為0到63，第二列為64到127，依此類推，最後一列為4032到4095。[cite: 12]]
[圖片描述：圖20 Frame Buffer。這是一個記憶體結構圖，位址從0編號到4095，圖下方標示每一筆資料為12bits。[cite: 12]]

本顯示引擎的任務是將所有的4096 pixel的RGB資訊儲存在 Frame Buffer中。[cite: 12]

2.4 時序規格[cite: 12]
2.4.1 Host 端時序圖[cite: 12]

[時序圖描述：此為Host端時序圖，包含下列訊號線與時序點：[cite: 12]
- clk: 系統時脈，週期標示為 T_CYCLE。[cite: 12]
- reset: 在T1期間發出一個高電位脈衝。[cite: 12]
- ready: 在reset之後拉低。於T2之前拉高，並在sprite輸入後降為低電位。標示了輸出setup time (T_OS)與 hold time (T_OH)。[cite: 12]
- sprite: 在T2期間產生高電位脈衝，標示了輸入setup time (T_IS)與 hold time (T_IH)。[cite: 12]
- X[7:0]、Y[7:0]、type[1:0]: 在T2的sprite脈衝期間，送出有效資料。[cite: 12]
- start: 在T3期間產生一個高電位脈衝。[cite: 12]
- done: 在T4期間產生一個高電位脈衝。[cite: 12]]

1. T1 時間點,系統啟動送出重置(reset)訊號,SGDE進行前置工作準備,準備前置工作期間ready 設為low。[cite: 13]
2. T2時間點,Host發現 ready為high,表示SGDE可以開始接受輸入,host 在clock 負緣送出第一筆物件資料,包含物件種類(type)及物件座標(X,Y),送出物件資料的同時,sprite 升為 high 表示此時物件資料有效。[cite: 13]SGDE若無法立刻處理此物件,即將ready設為low。[cite: 13]
3. T2至T3之間,Host以相同方式送出多筆物件資料[cite: 13]
4. T3時間點,物件資料都已送出,此時ready為high,Host送出一個cycle的start 訊號。[cite: 13]
5. SGDE收到start後,進行畫面最後繪製(寫入Frame Buffer),完成後送出done訊號(T4)。[cite: 13]

2.4.2 Host 端時序規格[cite: 13]
| Symbol[cite: 13] | Description[cite: 13] | Value[cite: 13] |
| :--- | :--- | :--- |
| T_cycle[cite: 13] | clock period[cite: 13] | user defined[cite: 13] |
| T_IS[cite: 13] | setup time (time period from valid input signal to positive edge of clock) for all inputs from host.[cite: 13] | 1/2 clock period[cite: 13] |
| T_IH[cite: 13] | hold time(time period from positive edge of clock to invalid input signal.) for all inputs from host[cite: 13] | 1/2 clock period[cite: 13] |
| T_OS[cite: 13] | setup time (from the time at which output is valid to the clock rising edge) for all outputs to the host[cite: 13] | 1/4 clock period[cite: 13] |
| T_OH[cite: 13] | hold time (from the clock rising edge to the time at which output is invalid) for all outputs to the host[cite: 13] | 0.1 ns[cite: 13] |

2.4.3 Frame Buffer 時序圖[cite: 13]
Frame Buffer Read: (FB_CEN=0)[cite: 13]
[時序圖描述：此為Frame Buffer Read時序圖，包含：[cite: 13]
- CLK: 系統時脈。[cite: 13]
- FB_WEN: 維持高電位。標示了 setup time (t_WS = 1ns) 與 hold time (t_WH = 0.5ns)。[cite: 13]
- FB_A: 送出Address1, Address2, Address3。標示了 setup time (t_AS = 1ns) 與 hold time (t_AH = 0.5ns)。[cite: 13]
- FB_Q: 在Address1給定後，經過 t_A = 1ns (Access time)，輸出Data1。接著依次輸出Data2, Data3。[cite: 13]]

Frame Buffer Write: (FB_CEN=0)[cite: 14]
[時序圖描述：此為Frame Buffer Write時序圖，包含：[cite: 14]
- CLK: 系統時脈。[cite: 14]
- FB_WEN: 產生低電位脈衝以寫入資料。標示了 t_WS = 1ns 與 t_WH = 0.5ns。[cite: 14]
- FB_A: 依次送出Address1, Address2, Address3。標示了 t_AS = 1ns 與 t_AH = 0.5ns。[cite: 14]
- FB_D: 依次寫入Data1, Data2, Data3。標示了 t_DS = 1ns (Data input setup time) 與 t_DH = 0.5ns (Data input hold time)。[cite: 14]
- FB_Q: 在寫入期間，對應位址的輸出訊號Data1, Data2, Data3 (Access time t_A = 1ns)。[cite: 14]]

| Symbol[cite: 14] | Description[cite: 14] | Delay(ns)[cite: 14] | Notes[cite: 14] |
| :--- | :--- | :--- | :--- |
| t_AS[cite: 14] | Address setup time[cite: 14] | 1[cite: 14] | Min[cite: 14] |
| t_AH[cite: 14] | Address hold time[cite: 14] | 0.5[cite: 14] | Min[cite: 14] |
| t_WS[cite: 14] | Write Enable Bar(FB_WEN) setup time[cite: 14] | 1[cite: 14] | Min[cite: 14] |
| t_WH[cite: 14] | Write Enable Bar(FB_WEN) hold time[cite: 14] | 0.5[cite: 14] | Min[cite: 14] |
| t_A[cite: 14] | Access time[cite: 14] | 1[cite: 14] | Max[cite: 14] |
| t_DS[cite: 14] | Data input setup time[cite: 14] | 1[cite: 14] | Min[cite: 14] |
| t_DH[cite: 14] | Data input hold time[cite: 14] | 0.5[cite: 14] | Min[cite: 14] |

2.4.4 Sprite Rom 時序圖[cite: 14]
Sprite ROM Read: (SR_CEN=0)[cite: 14]
[時序圖描述：此為Sprite ROM Read時序圖，包含：[cite: 14]
- CLK: 系統時脈。[cite: 14]
- SR_A: 依次送出Address1, Address2, Address3。標示了 t_AS = 1ns 與 t_AH = 0.5ns。[cite: 14]
- SR_Q: 經過 Access time t_A = 1ns 後，依次輸出Data1, Data2, Data3。[cite: 14]]

| Symbol[cite: 14] | Description[cite: 14] | Delay(ns)[cite: 14] | Notes[cite: 14] |
| :--- | :--- | :--- | :--- |
| t_AS[cite: 14] | Address setup time[cite: 14] | 1[cite: 14] | Min[cite: 14] |
| t_AH[cite: 14] | Address hold time[cite: 14] | 0.5[cite: 14] | Min[cite: 14] |
| t_A[cite: 14] | Access time[cite: 14] | 1[cite: 14] | Max[cite: 14] |

3. 評分標準[cite: 15]
課程助教將依照各組同學提供之系統時脈進行RTL simulation、gate-level simulation與 post-layout simulation,以驗證設計正確性。[cite: 15]各組同學應於本專案定義的系統時脈下,確保輸出結果無設置與保持時間(setup/hold time)的問題,並完全符合課程助教所提供的標準設計結果為準。[cite: 15]
表三為本次專案的評分標準,一旦設計經課程助教驗證後,且同學的設計結果正確(post-layout)將可得到基本分80分,而其次課程助教將依據 Performance Metric的表現,來給予而外的分數,而Performance Metric的計算方式如下所示,其數值越低表示設計的電路效能越好。[cite: 15]
• Performance Metric = Time Complexity Area Complexity[cite: 15]
Time Complexity:當你完成 Post layout 的 gate level netlist 的ncverilog的驗證時,你可以從“ncverilog.log”的檔案中得到你的Time Complexity結果,如圖十一所示。[cite: 15]這個時間代表整個電路完成工作所需的時間,與電路運作頻率(operating frequency)及電路效能(operations per cycle)有關。[cite: 15]
Area Complexity: Area Complexity可以從SOC encounter 的Layout圖透過尺規量得,如圖21所示。[cite: 15]

[圖片描述：圖21 Layout 尺規量測圖。這是一張SOC Encounter軟體的佈局截圖。[cite: 15]畫面左側顯示了電路佈局圖，外部環繞著綠色的Pad ring，內部為密集的紅綠藍佈線(Core區域)。[cite: 15]畫面上方標示了紅字「Core size: 445.74um * 442.89um」。[cite: 15]畫面右下角有一個放大顯示的工具列截圖，強調點選「Create Ruler (K) (F3 for setting)」，並提示「Please use Create Ruler.」。[cite: 15]]

附錄[cite: 15]
在附錄A 說明課程助教提供的遊戲物件的RGB 資訊;附錄B 為課程助教所提供各組同學的設計檔說明;附錄C為課程助教提供的測試樣本;附錄D為設計驗證說明;附錄E為評分用檔案,亦即各組同學必須繳交的檔案資料;附錄F為檔案上傳相關說明。[cite: 15]

附錄A遊戲物件 RGB 資訊[cite: 16]
課程助教提供的遊戲物件的RGB資訊紀錄如下(以16進位表示)。[cite: 16]

1. Man_live[cite: 16]
FFF FFF FE4 FFF FE5 982 FE9 FF5[cite: 16]
CC3 FE6 FF5 FFO FE6 FF5 FFO FD7[cite: 16]
FF5 CC3 FFF FE4 982 FFF FFF FE4[cite: 16]
FF4 FF4 871 FFO FFO 871 FF4 FF4[cite: 16]
FF3 FF3 FF3 FFO FFO FF3 FF3 FF3[cite: 16]
FE3 FFO FDB D86 D86 FDB FFO FE3[cite: 16]
FFF FF0 FFO DCB DCB FF0 FFO FFF[cite: 16]
FFF FFF EB5 FC6 FC6 DB5 FFF FFF[cite: 16]

2. Man_happy[cite: 16]
FFF FFF FE9 FE6 FE6 FD7 FFF FFF[cite: 16]
FFF FE5 FF5 FF5 FF5 FF5 FE4 FFF[cite: 16]
FE4 FFO 000 FFO FFO 000 FFO FE4[cite: 16]
FF4 FF0 FF0 FFO FFO FFO FFO FF4[cite: 16]
FF3 FF3 FF3 FFO FFO FF3 FF3 FF3[cite: 16]
FE3 FFO FFF FFF FFF FFF FFO FE3[cite: 16]
FFF FFO FFO FFF FFF FFO FFO FFF[cite: 16]
FFF FFF EB5 FC6 FC6 DB5 FFF FFF[cite: 16]

3. Man die[cite: 17]
FFF FFF BDF BDF BDF BDF FFF FFF[cite: 17]
FFF ACF ACF ACF ACF ACF ACF FFF[cite: 17]
9BF 9BF 79C 9BF 9BF 79C 9BF 9BF[cite: 17]
6AF 6AF 47A 6AF 6AF 47A 6AF 6AF[cite: 17]
6AF 6AF 6AF 6AF 47C 6AF 6AF 6AF[cite: 17]
6AF 6AF 47C 6AF 6AF 6AF 6AF 6AF[cite: 17]
FFF 9DF 7AC 58A 58A 7AC 9DF FFF[cite: 17]
FFF AEF AEF AEF AEF AEF AEF FFF[cite: 17]

4. Ghost[cite: 17]
FFF FFF FFF F90 F90 FFF FFF FFF[cite: 17]
FFF FFF F90 F90 F90 F90 FFF FFF[cite: 17]
FFF F90 FFF F90 F90 FFF F90 FFF[cite: 17]
F90 F90 F90 F90 F90 F90 F90 F90[cite: 17]
F90 F90 F90 F90 F90 F90 F90 F90[cite: 17]
F90 F90 F90 F90 F90 F90 F90 F90[cite: 17]
FFF F90 FFF F90 F90 FFF F90 FFF[cite: 17]
FFF F90 FFF FFF FFF FFF F90 FFF[cite: 17]

5. Candy[cite: 18]
FFF FFF FFF F43 F86 FFF FFF FFF[cite: 18]
FFF FFF B12 F80 F80 DFO FFF FFF[cite: 18]
FFF F90 F61 FCO CEO 3A3 177 FFF[cite: 18]
FFF FEE EEO FEO 7C2 394 FEE FFF[cite: 18]
FFF FFF 185 466 11030 100 FFF FFF[cite: 18]
FFF FFF F77 FAA 629 C8A FFF FFF[cite: 18]
FFF FFF FFF FOO FOO FFF FFF FFF[cite: 18]
FFF FFF FFF B12 E12 FFF FFF FFF[cite: 18]

6. Flower[cite: 18]
FFF FFF FFF FA3 FA3 FFF FFF FFF[cite: 18]
FFF FFF F90 F50 F50 F90 FFF FFF[cite: 18]
FFF F50 FAI FF0 FFO F50 FB7 FFF[cite: 18]
FFF FCB F50 FFO FEO F82 FFF FFF[cite: 18]
FFF FFF FCB F80 FDD FFF FFF 3B3[cite: 18]
CEC 8C8 FFF 060 3B3 FFF 060 FFF[cite: 18]
FFF 3B3 060 050 050 060 FFF FFF[cite: 18]
FFF FFF FFF 080 060 FFF FFF FFF[cite: 18]

附錄B設計檔(For verilog or VHDL)[cite: 19]
1. 下表為課程助教所提供各組同學的設計檔[cite: 19]
表二[cite: 19]

| 檔名[cite: 19] | 說明[cite: 19] |
| :--- | :--- |
| testfixture.v[cite: 19] | 測試樣本檔。此測試樣本檔定義了時脈週期與測試樣本之輸入信號[cite: 19] |
| Encounter_example[cite: 19] | SOC Encounter的範例檔(包含mmmc, LEF, GDS 生成指令......)[cite: 19] |
| SGDE.v[cite: 19] | 參賽者所使用的設計檔,已包含系統輸/出入埠之宣告[cite: 19] |
| FB.v[cite: 19] | Frame Buffer 的verilog 檔[cite: 19] |
| SR.v[cite: 19] | Sprite ROM的verilog 檔[cite: 19] |
| SR_verilog.rcf[cite: 19] | Sprite ROM 資料[cite: 19] |
| synopsys_dc.setup[cite: 19] | Design Compiler 設定檔[cite: 19] |
| SGDE.tcl[cite: 19] | The script for Design Compiler constraint[cite: 19] |
| input1.dat[cite: 19] | 第一組測試樣本之指令[cite: 19] |
| golden_image1.xpm[cite: 19] | 第一組測試樣本使用之影像檔[cite: 19] |
| goldenl.dat[cite: 19] | 第一組測試樣本之正確結果[cite: 19] |
| input2.dat[cite: 19] | 第二組測試樣本之指令[cite: 19] |
| golden_image2.xpm[cite: 19] | 第二組測試樣本使用之影像檔[cite: 19] |
| golden2.dat[cite: 19] | 第二組測試樣本之正確結果[cite: 19] |
| input3.dat[cite: 19] | 第三組測試樣本之指令[cite: 19] |
| golden_image3.xpm[cite: 19] | 第三組測試樣本使用之影像檔[cite: 19] |
| golden3.dat[cite: 19] | 第三組測試樣本之正確結果[cite: 19] |
| input4.dat[cite: 19] | 第四組測試樣本之指令[cite: 19] |
| golden_image4.xpm[cite: 19] | 第四組測試樣本使用之影像檔[cite: 19] |
| golden4.dat[cite: 19] | 第四組測試樣本之正確結果[cite: 19] |
| input5.dat[cite: 19] | 第五組測試樣本之指令[cite: 19] |
| golden_image5.xpm[cite: 19] | 第五組測試樣本使用之影像檔[cite: 19] |
| golden5.dat[cite: 19] | 第五組測試樣本之正確結果[cite: 19] |

2. 請使用SGDE.v,進行遊戲引擎控制器之設計。其模組名稱、輸出/入埠宣告如下所示:[cite: 19]
```
module SGDE (ready, done, clk, reset, sprite, start, type, X, Y, SR_CEN, SR_A, SR_Q, FB_CEN, FB_WEN, FB_A, FB_D, FB_Q);[cite: 19]
input clk, reset, sprite, start;[cite: 19]
input [1:0] type;[cite: 19]
input [5:0] X, Y;[cite: 20]
input [12:0] SR_Q;[cite: 20]
input [11:0] FB_Q;[cite: 20]
output ready, done;[cite: 20]
output SR_CEN, FB_CEN, FB_WEN;[cite: 20]
output [8:0] SR_A;[cite: 20]
output [11:0] FB_A, FB_D;[cite: 20]
endmodule[cite: 20]
```
3. 此專案共提供五組測試樣本,請自行修改testfixture.v內容,以使用其它組測試樣本,修改方法如下(以第二組為例):[cite: 20]
```
`define INDATAFILE "./input1.dat"[cite: 20]
`define GOLDENDATA "./goldenl.dat"[cite: 20]
`define OUTIMAGE "./image1.xpm"[cite: 20]
```
將以上三行修改為:[cite: 20]
```
`define INDATAFILE "./input2.dat"[cite: 20]
`define GOLDENDATA "./golden2.dat"[cite: 20]
`define OUTIMAGE "./image2.xpm"[cite: 20]
```
4. 使用verilog 模擬之後,會產生一個影像檔image1.xpm (OUTIMAGE 定義之檔名)。[cite: 20]此影像檔為模擬遊戲顯示器之輸出結果,請執行底下的UNIX指令以觀看此影像:[cite: 20]
> xv image1.xpm &[cite: 20]

5. 請各組同學使用課程助教所提供的測試樣本(testfixture.v),來進行設計之模擬驗證。[cite: 20]各組同學除了將利用本試題所提供的測試樣本進行各組同學之設計測試外,我們亦準備另一份測試樣本,進行設計之第二次驗證。[cite: 20]唯通過二次驗證且均能符合輸出結果,始能稱為無誤之設計。[cite: 20]
測試樣本(testfixture.v) 包含 clk, reset, sprite, start, 與 type 測試信號。[cite: 20]而於測試樣本內所定義的時脈週期參數(CYCLE),可由同學們依需求自行修改。[cite: 20]

附錄C測試樣本[cite: 20]
此專案共提供五組測試樣本,為方便設計者除錯之用,課程助教提供的遊戲物件座標以及遊戲物件型式詳列如下:[cite: 20]
Man(type=0) Ghost(type=l) Candy(type=2) Flower(type=3)[cite: 20]

1. 測試樣本一:input1.dat[cite: 21]

| 順序[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 4[cite: 21] | 5[cite: 21] | 6[cite: 21] | 7[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 0[cite: 21] | 1[cite: 21] | 2[cite: 21] | 1[cite: 21] | 3[cite: 21] | 2[cite: 21] | 3[cite: 21] |
| X[cite: 21] | 23[cite: 21] | 8[cite: 21] | 30[cite: 21] | 53[cite: 21] | 53[cite: 21] | 35[cite: 21] | 8[cite: 21] |
| Y[cite: 21] | 46[cite: 21] | 43[cite: 21] | 18[cite: 21] | 10[cite: 21] | 50[cite: 21] | 40[cite: 21] | 10[cite: 21] |

2. 測試樣本二:input2.dat[cite: 21]

| 順序[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 4[cite: 21] | 5[cite: 21] | 6[cite: 21] | 7[cite: 21] | 8[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 0[cite: 21] | 3[cite: 21] | 1[cite: 21] | 2[cite: 21] | 2[cite: 21] | 3[cite: 21] | 1[cite: 21] | 3[cite: 21] |
| X[cite: 21] | 30[cite: 21] | 48[cite: 21] | 31[cite: 21] | 25[cite: 21] | 36[cite: 21] | 49[cite: 21] | 35[cite: 21] | 28[cite: 21] |
| Y[cite: 21] | 30[cite: 21] | 53[cite: 21] | 23[cite: 21] | 26[cite: 21] | 28[cite: 21] | 49[cite: 21] | 35[cite: 21] | 33[cite: 21] |

3. 測試樣本三:input3.dat[cite: 21]

| 順序[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 4[cite: 21] | 5[cite: 21] | 6[cite: 21] | 7[cite: 21] | 8[cite: 21] | 9[cite: 21] | 10[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 0[cite: 21] | 1[cite: 21] | 3[cite: 21] | 2[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 2[cite: 21] | 1[cite: 21] | 3[cite: 21] |
| X[cite: 21] | 30[cite: 21] | 55[cite: 21] | 48[cite: 21] | 17[cite: 21] | 30[cite: 21] | 25[cite: 21] | 28[cite: 21] | 36[cite: 21] | 35[cite: 21] | 18[cite: 21] |
| Y[cite: 21] | 30[cite: 21] | 8[cite: 21] | 53[cite: 21] | 20[cite: 21] | 23[cite: 21] | 27[cite: 21] | 33[cite: 21] | 28[cite: 21] | 35[cite: 21] | 43[cite: 21] |

4. 測試樣本四:input4.dat[cite: 21]

| 順序[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 4[cite: 21] | 5[cite: 21] | 6[cite: 21] | 7[cite: 21] | 8[cite: 21] | 9[cite: 21] | 10[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 0[cite: 21] | 1[cite: 21] | 3[cite: 21] | 3[cite: 21] | 2[cite: 21] | 3[cite: 21] | 1[cite: 21] | 1[cite: 21] | 3[cite: 21] | 1[cite: 21] |
| X[cite: 21] | 30[cite: 21] | 5[cite: 21] | 23[cite: 21] | 18[cite: 21] | 7[cite: 21] | 8[cite: 21] | 25[cite: 21] | 41[cite: 21] | 48[cite: 21] | 33[cite: 21] |
| Y[cite: 21] | 30[cite: 21] | 8[cite: 21] | 12[cite: 21] | 43[cite: 21] | 20[cite: 21] | 13[cite: 21] | 5[cite: 21] | 23[cite: 21] | 53[cite: 21] | 34[cite: 21] |

| 順序[cite: 21] | 11[cite: 21] | 12[cite: 21] | 13[cite: 21] | 14[cite: 21] | 15[cite: 21] | 16[cite: 21] | 17[cite: 21] | 18[cite: 21] | 19[cite: 21] | 20[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 2[cite: 21] | 2[cite: 21] | 3[cite: 21] | 1[cite: 21] | 1[cite: 21] | 2[cite: 21] | 2[cite: 21] | 1[cite: 21] | 3[cite: 21] | 1[cite: 21] |
| X[cite: 21] | 21[cite: 21] | 25[cite: 21] | 28[cite: 21] | 5[cite: 21] | 31[cite: 21] | 47[cite: 21] | 54[cite: 21] | 5[cite: 21] | 9[cite: 21] | 21[cite: 21] |
| Y[cite: 21] | 2[cite: 21] | 27[cite: 21] | 35[cite: 21] | 35[cite: 21] | 3[cite: 21] | 2[cite: 21] | 56[cite: 21] | 55[cite: 21] | 55[cite: 21] | 43[cite: 21] |

5. 測試樣本五:input5.dat[cite: 21]

| 順序[cite: 21] | 1[cite: 21] | 2[cite: 21] | 3[cite: 21] | 4[cite: 21] | 5[cite: 21] | 6[cite: 21] | 7[cite: 21] | 8[cite: 21] | 9[cite: 21] | 10[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 0[cite: 21] | 3[cite: 21] | 1[cite: 21] | 2[cite: 21] | 2[cite: 21] | 3[cite: 21] | 2[cite: 21] | 1[cite: 21] | 1[cite: 21] | 3[cite: 21] |
| X[cite: 21] | 30[cite: 21] | 48[cite: 21] | 55[cite: 21] | 17[cite: 21] | 25[cite: 21] | 13[cite: 21] | 36[cite: 21] | 35[cite: 21] | 23[cite: 21] | 18[cite: 21] |
| Y[cite: 21] | 30[cite: 21] | 53[cite: 21] | 8[cite: 21] | 20[cite: 21] | 26[cite: 21] | 28[cite: 21] | 28[cite: 21] | 15[cite: 21] | 43[cite: 21] | 43[cite: 21] |

| 順序[cite: 21] | 11[cite: 21] | 12[cite: 21] | 13[cite: 21] | 14[cite: 21] | 15[cite: 21] | 16[cite: 21] | 17[cite: 21] | 18[cite: 21] | 19[cite: 21] | 20[cite: 21] |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| type[cite: 21] | 3[cite: 21] | 1[cite: 21] | 3[cite: 21] | 2[cite: 21] | 3[cite: 21] | 2[cite: 21] | 1[cite: 21] | 1[cite: 21] | 3[cite: 21] | 1[cite: 21] |
| X[cite: 21] | 48[cite: 21] | 55[cite: 21] | 17[cite: 21] | 15[cite: 21] | 28[cite: 21] | 47[cite: 21] | 35[cite: 21] | 5[cite: 21] | 47[cite: 21] | 20[cite: 21] |
| Y[cite: 21] | 3[cite: 21] | 4[cite: 21] | 2[cite: 21] | 26[cite: 21] | 33[cite: 21] | 27[cite: 21] | 35[cite: 21] | 54[cite: 21] | 20[cite: 21] | 26[cite: 21] |

[圖片描述：附錄D中提供之Verify Connectivity軟體操作截圖。[cite: 22]左側為設定視窗，右側為執行後的報告(PUB.conn.rpt)，紅色框線強調報告結尾處顯示「Found no problems or warnings.」。[cite: 22]]
[圖片描述：附錄D中提供之Verify Geometry軟體操作截圖。[cite: 22]左側為設定視窗，右側為執行後的報告(PUB.geom.rpt)，紅色框線強調報告結尾處顯示「No DRC violation were found」。[cite: 22]]

附錄D設計驗證說明[cite: 22]
各組同學繳交資料前應完成RTL,Gate-Level 與Physical 三種階段驗證,以確保設計正確性。[cite: 22]
* RTL 與Gate-Level階段:同學們必須進行RTL simulation 及Gate-Level simulation,模擬結果必須於參賽者自行定義的系統時脈下,輸出結果正確且無setup/hold time 的問題。[cite: 22]
* Physical 階段,包含三項驗證重點:[cite: 22]
    1.完成最後layout[cite: 22]
        i. Marco layout,不含IO Pad。[cite: 22]
        ii. VCC 與GND power ring 請各設定為2um。[cite: 22]
    2.完成post-layout simulation:參賽者必須使用P&R軟體寫出之 netlist 檔與sdf檔完成 post-layout gate-level simulation[cite: 22]
    3.完成DRC 與LVS驗證:參賽者必須以其所使用之P&R軟體內含之DRC 與LVS驗證功能完成DRC與LVS驗證[cite: 22]
        i 驗證 DRC 與LVS步驟如下: 在SOC Encounter 視窗下點選"Verify →Verify Connectivity... "Default值,按OK。[cite: 22]

在此步驟完成後會產生*.conn.rpt檔,如驗證結果正確,會顯示如右圖紅框處之文字。[cite: 22]
"Verify → Verify Geometry... " Default值,按OK。[cite: 22]
在此步驟完成後會產生*.geom.rpt檔,如驗證結果正確,會顯示如右圖紅框處之文字。[cite: 23]

附錄E評分用檔案[cite: 23]
評分所須檔案可以分為以下幾個部份:(1)RTL design,即各組同學對該次專案設計的RTL code,若設計採模組化而有多個設計檔,請務必將合成所要用到的各module檔放進來,以免助教進行評分時,無法進行模擬;(2)Gate-Level design,即由合成軟體所產生的 gate-level netlist,以及對應的SDF檔;(3)Physical design,即由Cadence SOC Encounter所產生的GDS Layout 檔,以及DRC/LVS report 檔等。[cite: 23]
表三[cite: 23]

| Design Stage[cite: 23] | File[cite: 23] | Description[cite: 23] |
| :--- | :--- | :--- |
| RTL category[cite: 23] |  |  |
| N/A[cite: 23] | *.doc[cite: 23] | Design Report Form[cite: 23] |
| RTL Simulation[cite: 23] | *.v or *.vhd[cite: 23] | Verilog (or VHDL) synthesizable RTL code[cite: 23] |
| Gate-Level category[cite: 23] |  |  |
| Pre-layout Gate-level[cite: 23] | *_syn.v <br> *_syn.sdf[cite: 23] | Verilog gate-level netlist generated by Synopsys Design Compiler <br> Pre-layout gate-level sdf[cite: 23] |
| Simulation[cite: 23] | *_syn.vcd[cite: 23] | Wave information generated by Ncverilog[cite: 23] |
| [cite: 23] | ncverilog.log[cite: 23] | The log file generated by Ncverilog[cite: 23] |
| Physical category[cite: 23] |  |  |
| P&R[cite: 23] | *.gds[cite: 23] | GDSII layout[cite: 23] |
| [cite: 23] | *.conn.rpt <br> *.geom.rpt[cite: 23] | DRC/LVS report generated by Cadence SOC Encounter[cite: 23] |
| Post-layout[cite: 23] | *_pr.v[cite: 23] | Verilog gate-level netlist generated by Cadence SOC Encounter[cite: 23] |
| Gate-level[cite: 23] | *_pr.sdf[cite: 23] | Post-layout gate-level sdf[cite: 23] |
| Simulation[cite: 23] | *_pr.vcd[cite: 23] | Wave information generated by Ncverilog[cite: 23] |
| [cite: 23] | ncverilog.log[cite: 23] | The log file generated by Ncverilog[cite: 23] |

附錄F檔案上傳[cite: 24]
所有包含於如附錄E中表格所示的檔案,均需要提交至課程助教指定的資料夾內。[cite: 24]步驟如下:[cite: 24]
1. 參考附錄E評分用檔案,上傳方式另行說明。[cite: 24]
請務必確保檔案放入正確的資料夾內。[cite: 24]
若有任何問題,請聯絡課程助教:[cite: 24]
李威 M11402117@mail.ntust.edu.tw[cite: 24]
辦公室: IB714-1[cite: 24]