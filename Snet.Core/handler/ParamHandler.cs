using Snet.Model.attribute;
using Snet.Model.data;
using Snet.Unility;
using System.Reflection;
using static Snet.Model.data.ParamModel;

namespace Snet.Core.handler
{
    /// <summary>
    /// 参数处理
    /// </summary>
    public class ParamHandler
    {
        /// <summary>
        /// 获取参数详情
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="obj">类型对象</param>
        /// <param name="name">名称</param>
        /// <param name="description">描述</param>
        /// <param name="properties">附加属性项集合；如果存在　AutoAllocatingTagAttribute　每个之类中都会添加　附加属性项集合</param>
        /// <returns>统一返回</returns>
        public static OperateResult Get<T>(T obj, string name, string description, List<propertie>? properties = null)
        {
            TimeHandler timeTool = TimeHandler.Instance(Guid.NewGuid().ToUpperNString());
            timeTool.StartRecord();
            try
            {
                //通过反射得到当前类的所有属性信息
                List<ReflexHandler.LibInstanceParam>? libInstanceParams = ReflexHandler.GetClassAllPropertyData<T>();
                //得到是否存在下标标识属性,并得到这个标识的枚举类型,这是用于自动分配属性的标识
                Tuple<Type, string, ReflexHandler.LibInstanceParam>? enumType = libInstanceParams.Select(c =>
                {
                    AutoAllocatingTagAttribute? indexesTagAttribute = typeof(T).GetProperty(c.Name).GetCustomAttribute<AutoAllocatingTagAttribute>();
                    if (indexesTagAttribute != null)
                    {
                        return new Tuple<Type, string, ReflexHandler.LibInstanceParam>(indexesTagAttribute.EnumType, c.Name, c);
                    }
                    return null;
                }).FirstOrDefault(c => c != null);
                //存在自动分配属性枚举　AutoAllocatingTagAttribute　特性
                if (enumType != null)
                {
                    //默认初始化一个父类
                    ParamModel? paramStructure = new ParamModel()
                    {
                        Name = name,
                        Description = description,
                        Subset = new List<subset>()
                    };
                    //获取枚举集合
                    Array array = Enum.GetValues(enumType.Item1);
                    for (int i = 0; i < array.Length; i++)
                    {
                        //搜索具有指定名称的公共字段
                        FieldInfo? field = enumType.Item1.GetField(array.GetValue(i).ToString());
                        //获取特性
                        AutoAllocatingAttribute? indexesAttribute = field?.GetCustomAttributes(typeof(AutoAllocatingAttribute), false)[0] as AutoAllocatingAttribute;
                        //检索枚举值　Item1 枚举值名称， Item2 详细描述
                        Tuple<string, string, object>? info = (enumType.Item3.EnumArray as List<dynamic>)?.Select(c =>
                        {
                            if (c.Name == array.GetValue(i).ToString())
                            {
                                return new Tuple<string, string, object>(c.Name, c.Describe, c.Value);
                            }
                            return null;
                        }).FirstOrDefault(c => c != null);

                        paramStructure.Subset.Add(new subset
                        {
                            Name = info.Item1,
                            Description = info.Item2,
                            Propertie = new List<propertie>()
                        });
                        //子集中需要添加项
                        if (properties != null && properties.Count > 0)
                        {
                            paramStructure.Subset[i].Propertie.AddRange(properties);
                        }
                        //在添加自身
                        paramStructure.Subset[i].Propertie.Add(new propertie
                        {
                            PropertyName = enumType.Item2,
                            Description = enumType.Item3.Describe,
                            Default = info.Item3,

                            Show = false,
                            Use = false,
                            MustFillIn = false,
                            DataCate = null,
                            DetailsTips = null,

                            Pattern = null,
                            FailTips = null,
                        });
                        //检索特性定义的属性名称
                        foreach (var item in indexesAttribute.PropertyNameArray)
                        {
                            ReflexHandler.LibInstanceParam? propertyInfo = libInstanceParams.FirstOrDefault(c => c.Name == item);
                            if (propertyInfo != null)
                            {
                                //默认值
                                string Default = ReflexHandler.GetModelValue(propertyInfo.Name, obj);
                                //前端展示特性
                                DisplayAttribute? displayAttribute = typeof(T).GetProperty(propertyInfo.Name).GetCustomAttribute<DisplayAttribute>();
                                //验证特性
                                VerifyAttribute? verifyAttribute = typeof(T).GetProperty(propertyInfo.Name).GetCustomAttribute<VerifyAttribute>();
                                //单位特性
                                UnitAttribute? unitAttribute = typeof(T).GetProperty(propertyInfo.Name).GetCustomAttribute<UnitAttribute>();
                                //描述上加单位
                                string Describe = propertyInfo.Describe;
                                if (unitAttribute != null && !string.IsNullOrWhiteSpace(unitAttribute.Unit))
                                {
                                    Describe += $"({unitAttribute.Unit})";
                                }

                                propertie propertie = new propertie
                                {
                                    PropertyName = propertyInfo.Name,
                                    Description = Describe,

                                    Show = displayAttribute?.Show ?? false,
                                    Use = displayAttribute?.Use ?? false,
                                    MustFillIn = displayAttribute?.MustFillIn ?? false,
                                    DataCate = displayAttribute?.DataCate ?? null,
                                    DetailsTips = displayAttribute?.DetailsTips ?? null,

                                    Pattern = verifyAttribute?.Pattern ?? null,
                                    FailTips = verifyAttribute?.FailTips ?? null,
                                };
                                switch (displayAttribute?.DataCate)
                                {
                                    case dataCate.select:
                                        propertie.Options = new List<options>();
                                        foreach (var val in propertyInfo.EnumArray as List<dynamic>)
                                        {
                                            string des = val.Describe;
                                            if (!string.IsNullOrEmpty(des))
                                            {
                                                des = $"({val.Describe})";
                                            }
                                            propertie.Options.Add(new options
                                            {
                                                Key = val.Name + des,
                                                Value = val.Value,
                                            });
                                        }
                                        break;

                                    case dataCate.radio:
                                        propertie.Options = new List<options>();
                                        propertie.Options.Add(new options
                                        {
                                            Key = "是",
                                            Value = true,
                                        });
                                        propertie.Options.Add(new options
                                        {
                                            Key = "否",
                                            Value = false,
                                        });
                                        break;
                                }
                                //默认值
                                propertie.Default = Default;
                                //添加当前子类
                                paramStructure.Subset[i].Propertie.Add(propertie);
                            }

                        }
                    }

                    //返回数据
                    return new OperateResult(true, paramStructure.ToJson(true), timeTool.StopRecord().milliseconds, paramStructure);
                }
                else
                {
                    //参数结构体
                    ParamModel? paramStructure = new ParamModel()
                    {
                        Name = name,
                        Description = description,
                        Subset = new List<subset>
                        {
                            new subset
                            {
                                Description = description,
                                Name = name,
                                Propertie = new List<propertie>()
                            }
                        }
                    };

                    //添加
                    if (properties != null && properties.Count > 0)
                    {
                        paramStructure.Subset[0].Propertie.AddRange(properties);
                    }

                    //检索参数
                    foreach (var lib in libInstanceParams)
                    {
                        //默认值
                        string Default = ReflexHandler.GetModelValue(lib.Name, obj);
                        //前端展示特性
                        DisplayAttribute? displayAttribute = typeof(T).GetProperty(lib.Name).GetCustomAttribute<DisplayAttribute>();
                        //验证特性
                        VerifyAttribute? verifyAttribute = typeof(T).GetProperty(lib.Name).GetCustomAttribute<VerifyAttribute>();
                        //单位特性
                        UnitAttribute? unitAttribute = typeof(T).GetProperty(lib.Name).GetCustomAttribute<UnitAttribute>();
                        //描述上加单位
                        string Describe = lib.Describe;
                        if (unitAttribute != null && !string.IsNullOrWhiteSpace(unitAttribute.Unit))
                        {
                            Describe += $"({unitAttribute.Unit})";
                        }

                        propertie propertie = new propertie
                        {
                            PropertyName = lib.Name,
                            Description = Describe,

                            Show = displayAttribute?.Show ?? false,
                            Use = displayAttribute?.Use ?? false,
                            MustFillIn = displayAttribute?.MustFillIn ?? false,
                            DataCate = displayAttribute?.DataCate ?? null,
                            DetailsTips = displayAttribute?.DetailsTips ?? null,

                            Pattern = verifyAttribute?.Pattern ?? null,
                            FailTips = verifyAttribute?.FailTips ?? null,
                        };

                        switch (displayAttribute?.DataCate)
                        {
                            case dataCate.select:
                                propertie.Options = new List<options>();
                                foreach (var val in lib.EnumArray as List<dynamic>)
                                {
                                    string des = val.Describe;
                                    if (!string.IsNullOrEmpty(des))
                                    {
                                        des = $"({val.Describe})";
                                    }
                                    propertie.Options.Add(new options
                                    {
                                        Key = val.Name + des,
                                        Value = val.Value,
                                    });
                                }
                                break;

                            case dataCate.radio:
                                propertie.Options = new List<options>();
                                propertie.Options.Add(new options
                                {
                                    Key = "是",
                                    Value = true,
                                });
                                propertie.Options.Add(new options
                                {
                                    Key = "否",
                                    Value = false,
                                });
                                break;
                        }
                        //添加默认值
                        propertie.Default = Default;
                        //添加当前项
                        paramStructure.Subset[0].Propertie.Add(propertie);
                    }

                    //返回数据
                    return new OperateResult(true, paramStructure.ToJson(true), timeTool.StopRecord().milliseconds, paramStructure);
                }
            }
            catch (Exception ex)
            {
                return new OperateResult(false, ex.Message, timeTool.StopRecord().milliseconds);
            }
        }
    }
}
