using Microsoft.ClearScript.V8;
using Snet.Core.extend;
using Snet.Model.data;
using static Snet.Core.script.ScriptData;
namespace Snet.Core.script
{
    /// <summary>
    /// 脚本操作
    /// </summary>
    public class ScriptOperate : CoreUnify<ScriptOperate, Basics>, IDisposable
    {
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public ScriptOperate() : base()
        {
            //实例化脚本引擎
            if (Engine == null)
            {
                Engine = new V8ScriptEngine();
            }
        }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="basics">基础数据</param>
        public ScriptOperate(Basics basics) : base(basics)
        {
            //实例化脚本引擎
            if (Engine == null)
            {
                Engine = new V8ScriptEngine();
            }
        }

        /// <inheritdoc/>

        public override void Dispose()
        {
            Engine.Dispose();
            Engine = null;
            base.Dispose();
        }

        /// <inheritdoc/>

        public override async Task DisposeAsync()
        {
            Dispose();
            await base.DisposeAsync();
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="ScriptParam">脚本参数</param>
        /// <returns>统一出参</returns>
        public OperateResult Execute(object[]? ScriptParam)
        {
            return Execute(basics.ScriptType, basics.ScriptCode, basics.ScriptFunction, ScriptParam);
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="ScriptParam">脚本参数</param>
        /// <returns>统一出参</returns>
        public async Task<OperateResult> ExecuteAsync(object[]? ScriptParam) => await Task.Run(() => Execute(ScriptParam));

        /// <summary>
        /// 脚本解析V8引擎
        /// </summary>
        private V8ScriptEngine Engine { get; set; }


        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="ScriptType">脚本类型</param>
        /// <param name="ScriptCode">脚本代码</param>
        /// <param name="ScriptFunction">脚本函数</param>
        /// <param name="ScriptParam">脚本参数</param>
        /// <returns>统一出参</returns>
        public OperateResult Execute(ScriptType ScriptType, string? ScriptCode, string? ScriptFunction, object[]? ScriptParam)
        {
            BegOperate();
            try
            {
                //返回参数
                string? RetParam = string.Empty;
                //执行脚本
                switch (ScriptType)
                {
                    case ScriptData.ScriptType.JavaScript:
                        Engine.Execute(ScriptCode);  //执行脚本
                        RetParam = Engine.Invoke(ScriptFunction, ScriptParam).ToString();
                        break;
                }
                return EndOperate(true, resultData: RetParam);
            }
            catch (Exception ex)
            {
                return EndOperate(false, ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="ScriptType">脚本类型</param>
        /// <param name="ScriptCode">脚本代码</param>
        /// <param name="ScriptFunction">脚本函数</param>
        /// <param name="ScriptParam">脚本参数</param>
        /// <returns>统一出参</returns>
        public async Task<OperateResult> ExecuteAsync(ScriptData.ScriptType ScriptType, string ScriptCode, string ScriptFunction, object[]? ScriptParam) => await Task.Run(() => Execute(ScriptType, ScriptCode, ScriptFunction, ScriptParam));
    }
}