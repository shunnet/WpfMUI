using Snet.Core.extend;
using Snet.Log;
using Snet.Model.@enum;
using Snet.Unility;
using System.Text;
using System.Text.RegularExpressions;
namespace Snet.Core.virtualAddress
{
    /// <summary>
    /// 虚拟地址
    /// </summary>
    public class VirtualAddress : CoreUnify<VirtualAddress, VirtualAddressData>, IDisposable
    {
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public VirtualAddress(VirtualAddressData basics) : base(basics)
        {
            try
            {
                if (token == null)
                {
                    token = new CancellationTokenSource();
                }
                string pattern = @"\{([^}]*)\}"; // 匹配 '{' 开始，'}' 结束之间的任何内容（不包括 '{' 和 '}'）  
                Match match = null;
                switch (basics.AddressType)
                {
                    case AddressType.VirtualDynamic_Random:
                        match = Regex.Match(basics.AddressName, pattern);
                        if (match.Success)
                        {
                            动态随机值(int.Parse(match.Groups[1].Value));
                        }
                        else
                        {
                            动态随机值();
                        }
                        break;
                    case AddressType.VirtualDynamic_RandomScope:
                        match = Regex.Match(basics.AddressName, pattern);
                        if (match.Success)
                        {
                            string[] item = match.Groups[1].Value.Split(',');
                            string[] minmax = item[1].Split('^');
                            int interval = int.Parse(item[0]);
                            string min = minmax[0];
                            string max = minmax[1];
                            动态随机范围值(min, max, interval);
                        }
                        else
                        {
                            动态随机范围值("", "");
                        }
                        break;
                    case AddressType.VirtualDynamic_Order:
                        match = Regex.Match(basics.AddressName, pattern);
                        if (match.Success)
                        {
                            string[] item = match.Groups[1].Value.Split(',');
                            int interval = int.Parse(item[0]);
                            float zzbl = float.Parse(item[1]);
                            动态顺序值(zzbl, interval);
                        }
                        else
                        {
                            动态顺序值(0);
                        }
                        break;
                    case AddressType.VirtualDynamic_OrderScope:
                        match = Regex.Match(basics.AddressName, pattern);
                        if (match.Success)
                        {
                            string[] item = match.Groups[1].Value.Split(',');
                            int interval = int.Parse(item[0]);
                            float zzbl = float.Parse(item[1]);
                            string[] minmax = item[2].Split('^');
                            string min = minmax[0];
                            string max = minmax[1];
                            动态顺序范围值(min, max, zzbl, interval);
                        }
                        else
                        {
                            动态顺序范围值("", "", 0);
                        }
                        break;
                    case AddressType.VirtualStatic:
                        //任何都不用操作
                        break;
                    default:
                        LogHelper.Error(message: $"虚拟地址 - [ {basics.AddressName} ]不支持虚拟地址操作", filename: "VirtualAddress.log");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"虚拟地址 - [ {basics.AddressName} ]初始化异常：{ex.Message}", "VirtualAddress.log", ex);
            }

        }
        /// <summary>
        /// 无惨构造函数
        /// </summary>
        public VirtualAddress() : base() { }

        /// <summary>
        /// 虚拟地址的值
        /// </summary>
        private object? Value { get; set; } = null;

        /// <summary>
        /// 读取
        /// </summary>
        /// <returns></returns>
        public object? Read()
        {
            return Value;
        }

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="value">写入的值</param>
        /// <returns></returns>
        public bool Write(object value)
        {
            Value = value;
            return true;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private CancellationTokenSource token;
        /// <summary>
        /// 随机
        /// </summary>
        private Random random = new Random();



        private async Task 动态随机值(int interval = 1000)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        switch (basics.DataType)
                        {
                            case DataType.Bool:
                                if (Value == null)
                                {
                                    Value = false;
                                }
                                else
                                {
                                    Value = !(bool)Value;
                                }
                                break;
                            case DataType.String:
                            case DataType.Char:
                                Value = 随机String类型(random.Next(1, 100));
                                break;
                            case DataType.Double:
                                Value = 随机Double类型();
                                break;
                            case DataType.Single:
                            case DataType.Float:
                                Value = 随机Float类型();
                                break;
                            case DataType.Int:
                            case DataType.Int32:
                                Value = 随机Int32类型();
                                break;
                            case DataType.Long:
                            case DataType.Int64:
                                Value = 随机Int64类型();
                                break;
                            case DataType.Short:
                            case DataType.Int16:
                                Value = 随机Int16类型();
                                break;
                            case DataType.Ulong:
                            case DataType.UInt64:
                                Value = 随机UInt64类型();
                                break;
                            case DataType.Uint:
                            case DataType.UInt32:
                                Value = 随机UInt32类型();
                                break;
                            case DataType.Ushort:
                            case DataType.UInt16:
                                Value = 随机UInt16类型();
                                break;
                            case DataType.DateTime:
                            case DataType.Date:
                                Value = 随机日期时间(DateTime.Parse("2000-01-01 00:00:00.000"), DateTime.Now);
                                break;
                            case DataType.Time:
                                Value = 随机时间(DateTime.Parse("2000-01-01 00:00:00.000"), DateTime.Now);
                                break;
                        }

                        await Task.Delay(interval, token.Token);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"虚拟地址 - [ {basics.AddressName} ]动态随机值异常：{ex.Message}", "VirtualAddress.log", ex);
                }
            }, token.Token);
        }
        private async Task 动态随机范围值(string min, string max, int interval = 1000)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        switch (basics.DataType)
                        {
                            case DataType.Bool:
                                if (Value == null)
                                {
                                    Value = false;
                                }
                                else
                                {
                                    Value = !(bool)Value;
                                }
                                break;
                            case DataType.String:
                            case DataType.Char:
                                Value = 随机String类型(random.Next(1, 100));
                                break;
                            case DataType.Double:
                                Value = 随机Double类型(double.Parse(min), double.Parse(max));
                                break;
                            case DataType.Single:
                            case DataType.Float:
                                Value = 随机Float类型(float.Parse(min), float.Parse(max));
                                break;
                            case DataType.Int:
                            case DataType.Int32:
                                Value = 随机Int32类型(Int32.Parse(min), Int32.Parse(max));
                                break;
                            case DataType.Long:
                            case DataType.Int64:
                                Value = 随机Int64类型(Int64.Parse(min), Int64.Parse(max));
                                break;
                            case DataType.Short:
                            case DataType.Int16:
                                Value = 随机Int16类型(Int16.Parse(min), Int16.Parse(max));
                                break;
                            case DataType.Ulong:
                            case DataType.UInt64:
                                Value = 随机UInt64类型(UInt64.Parse(min), UInt64.Parse(max));
                                break;
                            case DataType.Uint:
                            case DataType.UInt32:
                                Value = 随机UInt32类型(UInt32.Parse(min), UInt32.Parse(max));
                                break;
                            case DataType.Ushort:
                            case DataType.UInt16:
                                Value = 随机UInt16类型(UInt16.Parse(min), UInt16.Parse(max));
                                break;
                            case DataType.DateTime:
                            case DataType.Date:
                                Value = 随机日期时间(DateTime.Parse(min), DateTime.Parse(max));
                                break;
                            case DataType.Time:
                                Value = 随机时间(DateTime.Parse(min), DateTime.Parse(max));
                                break;
                        }

                        await Task.Delay(interval, token.Token);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"虚拟地址 - [ {basics.AddressName} ]动态随机范围值异常：{ex.Message}", "VirtualAddress.log", ex);
                }
            }, token.Token);
        }
        private async Task 动态顺序值(float zzbl, int interval = 1000)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        switch (basics.DataType)
                        {
                            case DataType.Bool:
                                if (Value == null)
                                {
                                    Value = false;
                                }
                                else
                                {
                                    Value = !(bool)Value;
                                }
                                break;
                            case DataType.String:
                            case DataType.Char:
                                Value = 随机String类型(random.Next(1, 100));
                                break;
                            case DataType.Double:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = double.Parse("0");
                                    }
                                    else
                                    {
                                        Value = Value.GetSource<double>() + zzbl;
                                    }
                                }
                                catch
                                {
                                    Value = double.Parse("0");
                                }
                                break;
                            case DataType.Single:
                            case DataType.Float:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = float.Parse("0");
                                    }
                                    else
                                    {
                                        Value = Value.GetSource<float>() + zzbl;
                                    }
                                }
                                catch
                                {
                                    Value = float.Parse("0");
                                }
                                break;
                            case DataType.Int:
                            case DataType.Int32:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int32.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (Int32)(Value.GetSource<Int32>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = Int32.Parse("0");
                                }
                                break;
                            case DataType.Long:
                            case DataType.Int64:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int64.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (Int64)(Value.GetSource<Int64>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = Int64.Parse("0");
                                }
                                break;
                            case DataType.Short:
                            case DataType.Int16:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int16.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (Int16)(Value.GetSource<Int16>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = Int16.Parse("0");
                                }
                                break;
                            case DataType.Ulong:
                            case DataType.UInt64:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt64.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (UInt64)(Value.GetSource<UInt64>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = UInt64.Parse("0");
                                }
                                break;
                            case DataType.Uint:
                            case DataType.UInt32:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt32.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (UInt32)(Value.GetSource<UInt32>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = UInt32.Parse("0");
                                }
                                break;
                            case DataType.Ushort:
                            case DataType.UInt16:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt16.Parse("0");
                                    }
                                    else
                                    {
                                        Value = (UInt16)(Value.GetSource<UInt16>() + zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = UInt16.Parse("0");
                                }

                                break;
                            case DataType.DateTime:
                            case DataType.Date:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = DateTime.Parse("2000-01-01 00:00:00.000");
                                    }
                                    else
                                    {
                                        Value = Value.GetSource<DateTime>().AddSeconds(zzbl);
                                        Value = Value.GetSource<DateTime>().AddDays(zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = DateTime.Parse("2000-01-01 00:00:00.000");
                                }
                                break;
                            case DataType.Time:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = DateTime.Parse("2000-01-01 00:00:00.000");
                                    }
                                    else
                                    {
                                        Value = Value.GetSource<DateTime>().AddSeconds(zzbl);
                                    }
                                }
                                catch
                                {
                                    Value = DateTime.Parse("2000-01-01 00:00:00.000");
                                }
                                break;
                        }

                        await Task.Delay(interval, token.Token);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"虚拟地址 - [ {basics.AddressName} ]动态顺序值异常：{ex.Message}", "VirtualAddress.log", ex);
                }
            }, token.Token);
        }
        private async Task 动态顺序范围值(string min, string max, float zzbl, int interval = 1000)
        {
            await Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        switch (basics.DataType)
                        {
                            case DataType.Bool:
                                if (Value == null)
                                {
                                    Value = false;
                                }
                                else
                                {
                                    Value = !(bool)Value;
                                }
                                break;
                            case DataType.String:
                            case DataType.Char:
                                Value = 随机String类型(random.Next(1, 100));
                                break;
                            case DataType.Double:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = double.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<double>() >= double.Parse(max))
                                        {
                                            Value = double.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<double>() + double.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = double.Parse(min);
                                }
                                break;
                            case DataType.Single:
                            case DataType.Float:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = float.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<float>() >= float.Parse(max))
                                        {
                                            Value = float.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<float>() + float.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = float.Parse(min);
                                }
                                break;
                            case DataType.Int:
                            case DataType.Int32:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int32.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<Int32>() >= Int32.Parse(max))
                                        {
                                            Value = Int32.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<Int32>() + Int32.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = Int32.Parse(min);
                                }
                                break;
                            case DataType.Long:
                            case DataType.Int64:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int64.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<Int64>() >= Int64.Parse(max))
                                        {
                                            Value = Int64.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<Int64>() + Int64.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = Int64.Parse(min);
                                }
                                break;
                            case DataType.Short:
                            case DataType.Int16:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = Int16.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<Int16>() >= Int16.Parse(max))
                                        {
                                            Value = Int16.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<Int16>() + Int16.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = Int16.Parse(min);
                                }
                                break;
                            case DataType.Ulong:
                            case DataType.UInt64:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt64.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<UInt64>() >= UInt64.Parse(max))
                                        {
                                            Value = UInt64.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<UInt64>() + UInt64.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = UInt64.Parse(min);
                                }
                                break;
                            case DataType.Uint:
                            case DataType.UInt32:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt32.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<UInt32>() >= UInt32.Parse(max))
                                        {
                                            Value = UInt32.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<UInt32>() + UInt32.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = UInt32.Parse(min);
                                }
                                break;
                            case DataType.Ushort:
                            case DataType.UInt16:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = UInt16.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<UInt16>() >= UInt16.Parse(max))
                                        {
                                            Value = UInt16.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<UInt16>() + UInt16.Parse(zzbl.ToString());
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = UInt16.Parse(min);
                                }
                                break;
                            case DataType.DateTime:
                            case DataType.Date:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = DateTime.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<DateTime>() >= DateTime.Parse(max))
                                        {
                                            Value = DateTime.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<DateTime>().AddSeconds(zzbl);
                                            Value = Value.GetSource<DateTime>().AddDays(zzbl);
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = DateTime.Parse(min);
                                }
                                break;
                            case DataType.Time:
                                try
                                {
                                    if (Value == null)
                                    {
                                        Value = DateTime.Parse(min);
                                    }
                                    else
                                    {
                                        if (Value.GetSource<DateTime>() >= DateTime.Parse(max))
                                        {
                                            Value = DateTime.Parse(min);
                                        }
                                        else
                                        {
                                            Value = Value.GetSource<DateTime>().AddSeconds(zzbl);
                                        }
                                    }
                                }
                                catch
                                {
                                    Value = DateTime.Parse(min);
                                }
                                break;
                        }

                        await Task.Delay(interval, token.Token);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"虚拟地址 - [ {basics.AddressName} ]动态顺序范围值异常：{ex.Message}", "VirtualAddress.log", ex);
                }
            }, token.Token);
        }


        private DateTime 随机时间(DateTime minTime, DateTime maxTime)
        {
            // 计算时间差（以秒为单位）  
            TimeSpan timeSpan = maxTime - minTime;
            double seconds = timeSpan.TotalSeconds;

            // 生成一个0到时间差之间的随机秒数  
            double randomSeconds = random.NextDouble() * seconds;

            // 将随机秒数转换回TimeSpan，并加到最小时间上  
            TimeSpan randomTimeSpan = TimeSpan.FromSeconds(randomSeconds);
            DateTime randomTime = minTime.Add(randomTimeSpan);
            return randomTime;
        }
        private DateTime 随机日期时间(DateTime minTime, DateTime maxTime)
        {
            // 转换为Ticks，因为Ticks是DateTime的最小时间单位，并且它是long类型，适合用于计算  
            long tickStart = minTime.Ticks;
            long tickEnd = maxTime.Ticks;
            long tickSpan = tickEnd - tickStart;

            // 生成一个0到tickSpan之间的随机long值  
            long randomTicks = tickStart + (long)(random.NextDouble() * tickSpan);

            // 将Ticks转换回DateTime  
            return new DateTime(randomTicks);
        }
        private short 随机Int16类型(short minValue = short.MinValue, short maxValue = short.MaxValue)
        {
            return (short)random.NextInt64(minValue, maxValue);
        }
        private ushort 随机UInt16类型(ushort minValue = ushort.MinValue, ushort maxValue = ushort.MaxValue)
        {
            return (ushort)random.NextInt64(minValue, maxValue);
        }
        private int 随机Int32类型(int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            return (int)random.NextInt64(minValue, maxValue);
        }
        private uint 随机UInt32类型(uint minValue = uint.MinValue, uint maxValue = uint.MaxValue)
        {
            return (uint)random.NextInt64(minValue, maxValue);
        }
        private long 随机Int64类型(long minValue = long.MinValue, long maxValue = long.MaxValue)
        {
            return random.NextInt64(minValue, maxValue);
        }
        private ulong 随机UInt64类型(ulong minValue = ulong.MinValue, ulong maxValue = long.MaxValue)
        {
            return (ulong)random.NextInt64((long)minValue, (long)maxValue);
        }
        private double 随机Double类型(double minValue = -999999.999999d, double maxValue = 999999.999999d)
        {
            // 计算范围  
            double range = maxValue - minValue;

            // 生成一个0（包含）到1（不包含）之间的随机double  
            double randomValue = random.NextDouble();

            // 缩放并偏移到所需范围  
            double result = minValue + (randomValue * range);

            // 如果需要，可以四舍五入到小数点后六位  
            result = Math.Round(result, 6);

            // 确保结果不是无穷大（虽然在这种情况下它本来就不会是）  
            if (double.IsInfinity(result))
            {
                result = 随机Double类型(minValue, maxValue);
            }

            return result;
        }
        private float 随机Float类型(float minValue = -999999.999999f, float maxValue = 999999.999999f)
        {
            // 计算范围  
            double range = maxValue - minValue;

            // 生成一个0（包含）到1（不包含）之间的随机double  
            double randomDouble = random.NextDouble();

            // 缩放并偏移到所需范围  
            double resultDouble = minValue + (randomDouble * range);

            // 四舍五入到小数点后六位（先转为decimal以提高精度）  
            decimal resultDecimal = Math.Round((decimal)resultDouble, 6);

            // 将结果转换回float（注意这里会有精度损失）  
            float resultFloat = (float)resultDecimal;

            // 确保结果不是无穷大（虽然在实际情况下它不会是）  
            if (float.IsInfinity(resultFloat))
            {
                resultFloat = 随机Float类型(minValue, maxValue);
            }

            return resultFloat;
        }
        private string 随机String类型(int length, string validCharacters = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            StringBuilder result = new StringBuilder();
            while (result.Length < length)
            {
                int index = random.Next(validCharacters.Length);
                char randomChar = validCharacters[index];
                result.Append(randomChar);
            }
            return result.ToString();
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (token != null)
            {
                token.Cancel();
                token = null;
            }
            base.Dispose();
        }

        /// <inheritdoc/>
        public override async Task DisposeAsync()
        {
            Dispose();
            await base.DisposeAsync();
        }
    }
}