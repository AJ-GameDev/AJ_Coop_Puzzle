/*           INFINITY CODE          */
/*     https://infinity-code.com    */

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public abstract class GenericInterceptor<T> : Interceptor where T : GenericInterceptor<T>
    {
        protected static T instance { get; private set; }

        protected override void Init()
        {
            instance = (T)this;

            base.Init();
        }
    }
}