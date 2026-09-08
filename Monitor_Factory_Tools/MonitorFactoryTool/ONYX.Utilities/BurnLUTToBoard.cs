using FDTI_Factory_i2c;
using MonitorFactoryTool.Pages;
using System;
using System.Windows;//zh add 250611
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FDTI_Factory_i2c.I2CController;
using System.Windows.Threading;
using Microsoft.Office.Interop.Excel;

namespace MonitorFactoryTool.ONYX.Utilities
{
    public class BurnLUTToBoard
    {
        private readonly I2CController _i2c;

        public BurnLUTToBoard(I2CController controller)
        {
            _i2c = controller ?? throw new ArgumentNullException(nameof(controller));
        }


        /// 一組 Gamma (gmCode) + 一組 CCT (ctCode) 的燒錄
        public async Task BurnOne(byte gmCode, byte ctCode, LUTData lut, double target_gammadicom, int target_CCT)
        {
            var blackWindow = System.Windows.Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            int idxMax = (int)(lut.mLUTEntry * 2) / 16; //wu add 250707
            // 改成在背景跑燒錄迴圈
            await Task.Run(async () =>
            {
                for (byte ch = 0; ch < 3; ch++)
                {
                    //for (byte idx = 0; idx < 32; idx++)
                    for (byte idx = 0; idx < idxMax; idx++)//zh 250707 (LUT_ENTRY_COUNT*2)/16
                    {
                        byte result;
                        int tries = 0;

                        string prefix = (gmCode == 0x20) ? "DICOM" : $"Gamma {target_gammadicom.ToString("F1")}";

                        // 用 InvokeAsync更新 UI 避免 idx ch 卡住
                        await blackWindow.Dispatcher.InvokeAsync(() => blackWindow.textblockStatus.Text = $"{prefix} {target_CCT} upload LUT idx: {idx} ch: {ch}");

                        do
                        {
                            _i2c.DDCCI_Null_Message();

                            Console.WriteLine($"Burn  {prefix}, CCT={target_CCT}, ch={ch}, idx={idx}, attempt={tries + 1}");

                            //result = _i2c.DDCCI_Set_CommandGammaCT(0x4E, 0x00, ch, idx, lut);
                            result = _i2c.DDCCI_Set_CommandGammaCT(gmCode, ctCode, ch, idx, lut);
                            await Task.Delay(100);

                            if (result == 255)//zh add 250611
                            {
                                Console.WriteLine($"Gamma Checksum 錯誤, 重試次數 {tries}!");//zh add 250611
                            }
                            else if (result != 0)
                            {
                                Console.WriteLine($"  → idx={idx}, ch={ch} 失敗，第 {tries + 1} 次重試");
                                await Task.Delay(100);   // 只在失敗時等一下
                            }
                            tries++;
                            //if (result == 255 && tries == 10)//zh add 250611
                            if (result == 255 && tries == 50)//zh add 251231
                            {
                                MessageBox.Show($"Gamma Checksum 錯誤，終止校正!");//zh add 250611
                            }
                            //  } while (result != 0 && tries <= 10);
                        } while (result != 0 && tries <= 50);//zh add 251231
                        
                        if (result != 0)
                            Console.WriteLine($"*** 重試" + tries + "仍然失敗: idx={idx}, ch={ch}");
                    }
                }
            });
        }


        //如果要一次燒全部的話用BurnSequentially
        public void BurnSequentially(
       IEnumerable<byte> gmCodes,
       IEnumerable<byte> ctCodes,
       Dictionary<(int cct, double gamma), LUTData> lutTable)
        {
            foreach (var gmCode in gmCodes)
            {
                // 先把 100% 完成校正的 log 印一次
                Console.WriteLine($"===== 開始燒錄 Gamma 0x{gmCode:X2} =====");

                foreach (var ctCode in ctCodes)
                {
                    // 將 ctCode 轉回整數 CCT
                    int cctInt;
                    switch (ctCode)
                    {
                        case I2CController.CT1:
                            cctInt = 5400;
                            break;
                        case I2CController.CT2:
                            cctInt = 6500;
                            break;
                        case I2CController.CT3:
                            cctInt = 9300;
                            break;
                        default:
                            throw new ArgumentException($"未知的 CCT code 0x{ctCode:X2}");
                    }

                    // 將 gmCode 轉回 double Gamma
                    double gammaVal;
                    switch (gmCode)
                    {
                        case I2CController.GAMA1:
                            gammaVal = 1.85;
                            break;
                        case I2CController.GAMA2:
                            gammaVal = 2.05;
                            break;
                        case I2CController.GAMA3:
                            gammaVal = 2.25;
                            break;
                        case I2CController.GAMA4:
                            gammaVal = 2.45;
                            break;
                        case I2CController.GAMA5:
                            gammaVal = 2.65;
                            break;
                        default:
                            throw new ArgumentException($"未知的 Gamma code 0x{gmCode:X2}");
                    }
                    if (!lutTable.TryGetValue((cctInt, gammaVal), out var lut))
                    {
                        Console.WriteLine($"[Error] 找不到 CCT={cctInt}K, Gamma={gammaVal:F2} 的 LUT");
                        continue;
                    }

                    Console.WriteLine($">>> 燒錄 CCT={cctInt}K, Gamma={gammaVal:F2}");
                    // BurnOne(gmCode, ctCode, lut);
                }
            }
            Console.WriteLine("[BurnLUTToBoard] 所有指定 Gamma × CCT 已依序燒錄完成");
        }
    }
}

