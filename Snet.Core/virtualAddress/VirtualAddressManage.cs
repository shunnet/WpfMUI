using Snet.Model.data;
using Snet.Model.@enum;
using System.Collections.Concurrent;

namespace Snet.Core.virtualAddress
{
    /// <summary>
    /// 虚拟地址统一管理<br/>
    /// 无法支持订阅模式下数据触发，只支持读取或写入(Read/Write)<br/>
    /// 第一次初始化是在读取时，会把所有虚拟点记录<br/>
    /// 只有初始化后写入才生效<br/>
    /// DAQ采集协议都支持<br/>
    /// 注意：不支持数组数据类型模拟
    /// </summary>
    public class VirtualAddressManage : IDisposable
    {
        /// <summary>
        /// 虚拟地址的容器
        /// </summary>
        private ConcurrentDictionary<string, (VirtualAddress virtualAddress, AddressType addressType, DataType dataType)> VirtualAddressIocContainer = new ConcurrentDictionary<string, (VirtualAddress virtualAddress, AddressType addressType, DataType dataType)>();

        /// <summary>
        /// 设置虚拟地址，存在则不做操作，不存在则添加
        /// </summary>
        /// <param name="addressData">虚拟地址的值</param>
        /// <returns></returns>
        private void SetVirtualAddress(VirtualAddressData addressData)
        {
            //是否需要添加虚拟点
            bool addVirtua = false;
            //查询得到结果
            int result = VirtualAddressIocContainer.Where(c => c.Key == addressData.AddressName).Where(c => c.Value.addressType != addressData.AddressType || c.Value.dataType != addressData.DataType).Count();
            //如果 结果 大于0 说明这个标签是被修改后的
            if (result > 0)
            {
                //得移除这个标签,在创建新的
                if (VirtualAddressIocContainer.Remove(addressData.AddressName, out (VirtualAddress virtualAddress, AddressType addressType, DataType dataType) aData))
                {
                    //移除成功,释放虚拟点基类
                    aData.virtualAddress.Dispose();
                    //设置添加虚拟点状态
                    addVirtua = true;
                }
            }
            else
            {
                addVirtua = true;
            }

            //判断是否需要添加虚拟点
            if (addVirtua)
            {
                add(new VirtualAddressData { AddressName = addressData.AddressName, AddressType = addressData.AddressType, DataType = addressData.DataType });
            }

        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="addressData">地址数据</param>
        private void add(VirtualAddressData addressData)
        {
            //实例化虚拟地址
            VirtualAddress virtualAddress = VirtualAddress.Instance(addressData);

            //设置value 
            (VirtualAddress virtualAddress, AddressType addressType, DataType dataType) dVlaue = (virtualAddress, addressData.AddressType, addressData.DataType);

            //添加虚拟地址数据
            VirtualAddressIocContainer.AddOrUpdate(addressData.AddressName, dVlaue, (k, v) => dVlaue);
        }

        /// <summary>
        /// 获取虚拟地址
        /// </summary>
        /// <param name="addressName">地址名称</param>
        /// <returns></returns>
        private VirtualAddress? GetVirtualAddress(string addressName)
        {
            if (VirtualAddressIocContainer != null)
            {
                return VirtualAddressIocContainer[addressName].virtualAddress;
            }
            return null;
        }

        /// <summary>
        /// 设置虚拟点
        /// </summary>
        public void InitVirtualAddress(AddressDetails details, out bool IsVA)
        {
            //判断是不是虚拟点
            if (IsVirtualAddress(details))
            {
                SetVirtualAddress(new VirtualAddressData() { AddressName = details.AddressName, DataType = details.AddressDataType, AddressType = details.AddressType });
                IsVA = true;
            }
            else
            {
                IsVA = false;
            }
        }

        /// <summary>
        /// 判断是不是虚拟地址
        /// </summary>
        public bool IsVirtualAddress(AddressDetails details)
        {
            return details.AddressType.Equals(AddressType.VirtualStatic) ||
                   details.AddressType.Equals(AddressType.VirtualDynamic_Random) ||
                   details.AddressType.Equals(AddressType.VirtualDynamic_RandomScope) ||
                   details.AddressType.Equals(AddressType.VirtualDynamic_Order) ||
                   details.AddressType.Equals(AddressType.VirtualDynamic_OrderScope);
        }

        /// <summary>
        /// 判断是不是虚拟地址
        /// </summary>
        public bool IsVirtualAddress(string AddressName)
        {
            if (VirtualAddressIocContainer.ContainsKey(AddressName))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 读取虚拟点的值
        /// </summary>
        public object? Read(AddressDetails details)
        {
            VirtualAddress? virtualAddress = GetVirtualAddress(details.AddressName);
            if (virtualAddress != null)
            {
                return virtualAddress.Read();
            }
            return null;
        }

        /// <summary>
        /// 读取虚拟点的值
        /// </summary>
        public object? Read(string AddressName)
        {
            VirtualAddress? virtualAddress = GetVirtualAddress(AddressName);
            if (virtualAddress != null)
            {
                return virtualAddress.Read();
            }
            return null;
        }

        /// <summary>
        /// 写入虚拟点的值
        /// </summary>
        public bool Write(AddressDetails details, object Value)
        {
            VirtualAddress? virtualAddress = GetVirtualAddress(details.AddressName);
            if (virtualAddress != null)
            {
                return virtualAddress.Write(Value);
            }
            return false;
        }

        /// <summary>
        /// 写入虚拟点的值
        /// </summary>
        public bool Write(string AddressName, object Value)
        {
            VirtualAddress? virtualAddress = GetVirtualAddress(AddressName);
            if (virtualAddress != null)
            {
                return virtualAddress.Write(Value);
            }
            return false;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (VirtualAddressIocContainer.Count > 0)
            {
                foreach (var item in VirtualAddressIocContainer)
                {
                    item.Value.virtualAddress.Dispose();
                }
            }
            VirtualAddressIocContainer.Clear();
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}