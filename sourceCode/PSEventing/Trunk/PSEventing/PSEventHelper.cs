using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Nivot.PowerShell.Eventing
{
    /// <summary>
    /// 
    /// </summary>
    internal static class PSEventHelper
    {
        internal const string METHOD_FORMAT = "PSEventHandler_{0}_{1}";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="eventName"></param>
        /// <param name="caseSensitive"></param>
        /// <returns></returns>
        internal static EventInfo GetEventInfo(Type type, string eventName, bool caseSensitive)
        {
            BindingFlags flags = (BindingFlags.Instance | BindingFlags.Public);
            if (caseSensitive == false)
            {
                flags |= BindingFlags.IgnoreCase;
            }

            EventInfo eventInfo = type.GetEvent(eventName, flags);

            return eventInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerType"></param>
        /// <param name="skipFirstParameter"></param>
        /// <returns></returns>
        internal static Type[] GetDelegateParameterTypes(Type handlerType, bool skipFirstParameter)
        {
            MethodInfo invoke = null;
            EnsureTypeIsDelegate(handlerType, ref invoke);

            ParameterInfo[] parameters = invoke.GetParameters();

            // we need to skip the first parameter if using an "instance" style dynamic method
            int startIndex = (skipFirstParameter) ? 1 : 0;

            Type[] typeParameters = new Type[parameters.Length];
            for (int i = startIndex ; i < parameters.Length ; i++)
            {
                typeParameters[i] = parameters[i].ParameterType;
            }

            return typeParameters;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerType"></param>
        /// <returns></returns>
        internal static Type GetDelegateReturnType(Type handlerType)
        {
            MethodInfo invoke = null;
            EnsureTypeIsDelegate(handlerType, ref invoke);

            return invoke.ReturnType;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handlerType"></param>
        /// <param name="invoke"></param>
        private static void EnsureTypeIsDelegate(Type handlerType, ref MethodInfo invoke)
        {
            string name = handlerType.Name;

            if (handlerType.BaseType != typeof(MulticastDelegate))
            {
                throw new ArgumentException(name + " is not a delegate.");
            }

            invoke = handlerType.GetMethod("Invoke");
            if (invoke == null)
            {
                throw new ArgumentException(name + " is not a delegate.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="eventInfo"></param>
        /// <param name="bindingID"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        internal static Delegate BindEventSink(PSVariable variable, EventInfo eventInfo, Guid bindingID, string queueName)
        {
            Type returnType = eventInfo.EventHandlerType;
            if (GetDelegateReturnType(returnType) != typeof(void))
            {
                // we don't support non-void return types yet.
                throw new NotSupportedException("The associated delegate must be multicast with void return type.");
            }

            // dynamic method name
            string methodName = String.Format(METHOD_FORMAT, variable.Name, eventInfo.Name);
            
            object target = PSVariableHelper.GetBaseObject(variable);
            //Type targetType = targetObject.GetType();           
            
            // set parameter true for instance-bound dynamicmethod
            Type[] parameterTypes = GetDelegateParameterTypes(eventInfo.EventHandlerType, false);

            // fetch a delegate to a dynamic event handler
            Delegate handlerDelegate = GetHandlerDelegate(eventInfo, methodName, parameterTypes, bindingID, queueName);

            // dynamically += dynamicmethod delegate 
            MethodInfo addHandler = eventInfo.GetAddMethod();
            addHandler.Invoke(target, new object[] { handlerDelegate });

            return handlerDelegate;
        }

        private static Delegate GetHandlerDelegate(EventInfo eventInfo, string methodName, Type[] parameterTypes, Guid bindingID, string queueName)
        {
            // construct dynamic method matching delegate signature
            // it will be void(object sender, EventArgs e)

            // NOTE: weird things happen if associated with PSQueueHelper
            //       we get nullref exceptions if you don't "warm up" the queue with an entry
            //       - perhaps weird static initialization glitches?

            DynamicMethod handlerMethod =
                new DynamicMethod(methodName,
                                  null,
                                  parameterTypes,
                                  typeof(DynamicMethodAnchor)); //targetType); // TODO: associate with bindingattribute

            // singleton fieldinfo
            Type eventQueueType = typeof(PSQueueHelper);            
            FieldInfo instance = eventQueueType.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(instance != null, "instance != null");

            // methodinfo
            MethodInfo getEventQueue = eventQueueType.GetMethod("GetEventQueue", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(getEventQueue != null, "getEventQueue != null");

            // methodinfo
            MethodInfo enqueueEvent = typeof(IPSEventQueue).GetMethod("EnqueueEvent");
            Debug.Assert(enqueueEvent != null, "enqueueEvent != null");

            // Debug.WriteLine(string, string)
            MethodInfo writeLine =
                typeof(Debug).GetMethod("WriteLine", new Type[2] { typeof(string), typeof(string) });

            // lets get dirty
            ILGenerator il = handlerMethod.GetILGenerator();
            
            /*
                .method public hidebysig static void  TestNamedQueue(object sender, class [mscorlib]System.EventArgs args) cil managed
                {
                  // Code size       35 (0x23)
                  .maxstack  8
                  IL_0000:  nop
                  IL_0001:  ldsfld     class Nivot.PowerShell.Eventing.PSQueueHelper Nivot.PowerShell.Eventing.PSQueueHelper::Instance
                  IL_0006:  ldstr      "queue"
                  IL_000b:  callvirt   instance class Nivot.PowerShell.Eventing.IPSEventQueue Nivot.PowerShell.Eventing.PSQueueHelper::GetEventQueue(string)
                  IL_0010:  ldstr      "bindingid"
                  IL_0015:  ldarg.0
                  IL_0016:  ldstr      "click"
                  IL_001b:  ldarg.1
                  IL_001c:  callvirt   instance void Nivot.PowerShell.Eventing.IPSEventQueue::EnqueueEvent(string, object, string, class [mscorlib]System.EventArgs)
                  IL_0021:  nop
                  IL_0022:  ret
                } // end of method NameQueues::TestNamedQueue
             */

            // ...
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldstr, "Enter");
            il.Emit(OpCodes.Ldstr, "LCG EventHandler");
            il.Emit(OpCodes.Call, writeLine);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldsfld, instance);
            il.Emit(OpCodes.Ldstr, queueName);
            il.Emit(OpCodes.Callvirt, getEventQueue);
            il.Emit(OpCodes.Ldstr, bindingID.ToString());
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, eventInfo.Name);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, enqueueEvent);
            il.Emit(OpCodes.Nop);
            
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldstr, "Exit");
            il.Emit(OpCodes.Ldstr, "LCG EventHandler");
            il.Emit(OpCodes.Call, writeLine);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);

            // wire up our emitted method to the target object
            return handlerMethod.CreateDelegate(eventInfo.EventHandlerType);
        }

        internal static void UnbindEventSink(PSVariable variable, string eventName, Delegate handler)
        {
            Debug.Assert(variable.Value != null, "variable.Value != null");

            object targetObject = ((PSObject)variable.Value).BaseObject;
            UnbindEventSinkFromTarget(targetObject, eventName, handler);
        }

        internal static void UnbindEventSinkFromTarget(object targetObject, string eventName, Delegate handler)
        {
            Debug.Assert(targetObject != null, "targetObject != null");

            Type targetType = targetObject.GetType();
            EventInfo eventInfo = GetEventInfo(targetType, eventName, false);

            MethodInfo removeHandler = eventInfo.GetRemoveMethod();
            removeHandler.Invoke(targetObject, new object[] { handler });            
        }

        // all dynamic methods are associated with this type
        private sealed class DynamicMethodAnchor {}
    }
}
