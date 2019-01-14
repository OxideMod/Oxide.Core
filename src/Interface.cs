using System;

namespace Oxide.Core
{
    /// <summary>
    /// The interface class through which patched DLLs interact with Oxide
    /// </summary>
    public static class Interface
    {
        /// <summary>
        /// Gets the main Oxide mod instance
        /// </summary>
        public static OxideMod Oxide { get; private set; }

        /// <summary>
        /// Gets or sets the debug callback to use
        /// </summary>
        public static NativeDebugCallback DebugCallback { get; set; }

        /// <summary>
        /// Initializes Oxide
        /// </summary>
        public static void Initialize()
        {
            // Create if not already created
            if (Oxide != null)
            {
                return;
            }

            Oxide = new OxideMod(DebugCallback);
            Oxide.Load();
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return Oxide.CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecated(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object[] args) => Oxide?.CallHook(hook, args);

        #region Hook Overloads

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hook)
        {
            return CallHook(hook, null);
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1)
        {
            object[] array = ArrayPool.Get(1);
            array[0] = obj1;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2)
        {
            object[] array = ArrayPool.Get(2);
            array[0] = obj1;
            array[1] = obj2;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3)
        {
            object[] array = ArrayPool.Get(3);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4)
        {
            object[] array = ArrayPool.Get(4);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5)
        {
            object[] array = ArrayPool.Get(5);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <param name="obj6"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5, object obj6)
        {
            object[] array = ArrayPool.Get(6);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            array[5] = obj6;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <param name="obj6"></param>
        /// <param name="obj7"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7)
        {
            object[] array = ArrayPool.Get(7);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            array[5] = obj6;
            array[6] = obj7;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <param name="obj6"></param>
        /// <param name="obj7"></param>
        /// <param name="obj8"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8)
        {
            object[] array = ArrayPool.Get(8);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            array[5] = obj6;
            array[6] = obj7;
            array[7] = obj8;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <param name="obj6"></param>
        /// <param name="obj7"></param>
        /// <param name="obj8"></param>
        /// <param name="obj9"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9)
        {
            object[] array = ArrayPool.Get(9);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            array[5] = obj6;
            array[6] = obj7;
            array[7] = obj8;
            array[8] = obj9;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="obj3"></param>
        /// <param name="obj4"></param>
        /// <param name="obj5"></param>
        /// <param name="obj6"></param>
        /// <param name="obj7"></param>
        /// <param name="obj8"></param>
        /// <param name="obj9"></param>
        /// <param name="obj10"></param>
        /// <returns></returns>
        public static object CallHook(string hook, object obj1, object obj2, object obj3, object obj4, object obj5, object obj6, object obj7, object obj8, object obj9, object obj10)
        {
            object[] array = ArrayPool.Get(10);
            array[0] = obj1;
            array[1] = obj2;
            array[2] = obj3;
            array[3] = obj4;
            array[4] = obj5;
            array[5] = obj6;
            array[6] = obj7;
            array[7] = obj8;
            array[8] = obj9;
            array[9] = obj10;
            object ret = CallHook(hook, array);
            ArrayPool.Free(array);
            return ret;
        }

        #endregion Hook Overloads

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Call(string hook, params object[] args) => CallHook(hook, args);

        /// <summary>
        /// Calls the specified hook and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Call<T>(string hook, params object[] args) => (T)Convert.ChangeType(CallHook(hook, args), typeof(T));

        /// <summary>
        /// Gets the Oxide mod
        /// </summary>
        /// <returns></returns>
        public static OxideMod GetMod() => Oxide;

        public static OxideMod uMod => Oxide;
    }
}
