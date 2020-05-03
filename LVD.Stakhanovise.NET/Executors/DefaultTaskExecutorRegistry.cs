using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace LVD.Stakhanovise.NET.Executors
{
   public class DefaultTaskExecutorRegistry
   {
      private Dictionary<Type, Type> mMessageExecutorTypes
            = new Dictionary<Type, Type>();

      private Dictionary<Type, PropertyInfo[]> mMessageExecutorInjectableProperties
          = new Dictionary<Type, PropertyInfo[]>();

      private Dictionary<string, Type> mPayloadTypes
          = new Dictionary<string, Type>();

      private Func<Type, object> mDependencyResolverFn;

      private static Type mExecutorInterface = 
         typeof(ITaskExecutor<>);

      public DefaultTaskExecutorRegistry(Func<Type, object> dependencyResolverFn)
      {
         mDependencyResolverFn = dependencyResolverFn ?? throw new ArgumentNullException(nameof(dependencyResolverFn));
      }

      private Type GetImplementedExecutorInterface(Type type)
      {
         if (!type.IsClass || type.IsAbstract)
            return null;

         return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType
             && mExecutorInterface.IsAssignableFrom(i.GetGenericTypeDefinition()));
      }

      private bool IsInjectableProperty(PropertyInfo propertyInfo)
      {
         Type propertyType = propertyInfo.PropertyType;
         //We consider an injectable type as being anything that is not:
         //  - a primitive;
         //  - a value type;
         //  - an array;
         //  - a string type.
         return !propertyType.IsPrimitive
             && !propertyType.IsValueType
             && !propertyType.IsArray
             && !propertyType.Equals(typeof(string));
      }

      private void ScanAssembly(Assembly assembly)
      {
         Type[] executorTypes = assembly.GetTypes();

         foreach (Type candidateType in executorTypes)
         {
            //See if the candidate type implements ITaskExecutor<> and that is a non-abstract class;
            //  if not, skip it
            Type implementedInterface = GetImplementedExecutorInterface(candidateType);
            if (implementedInterface == null)
               continue;

            //Fetch the generic argument - this is the payload type
            Type payloadType = implementedInterface.GenericTypeArguments.FirstOrDefault();
            if (payloadType == null)
               continue;

            PropertyInfo[] injectableProperties = candidateType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && IsInjectableProperty(p))
                .ToArray();

            //Register these two pieces of information for later use
            mPayloadTypes[payloadType.FullName] = payloadType;
            mMessageExecutorTypes[payloadType] = candidateType;

            if (injectableProperties != null && injectableProperties.Length > 0)
               mMessageExecutorInjectableProperties[candidateType] = injectableProperties;
         }
      }

      public void ScanAssemblies(params Assembly[] assemblies)
      {
         if (assemblies != null && assemblies.Length > 0)
         {
            foreach (Assembly assembly in assemblies)
            {
               if (assembly != null)
                  ScanAssembly(assembly);
            }
         }
      }

      public ITaskExecutor<TPayload> ResolveExecutor<TPayload>()
      {
         return ResolveExecutor(payloadType: typeof(TPayload))
             as ITaskExecutor<TPayload>;
      }

      public ITaskExecutor ResolveExecutor(Type payloadType)
      {
         Type executorType;

         PropertyInfo[] injectableProperties;
         ITaskExecutor executorInstance;

         if (mMessageExecutorTypes.TryGetValue(payloadType, out executorType))
         {
            //Create executor instance, if a type is found for the payload type
            executorInstance = (ITaskExecutor)Activator
                .CreateInstance(executorType);

            //If we have any injectable properties, 
            //  attempt to resolve values and inject them accordingly
            if (mMessageExecutorInjectableProperties.TryGetValue(executorType, out injectableProperties))
            {
               foreach (PropertyInfo prop in injectableProperties)
                  prop.SetValue(executorInstance, mDependencyResolverFn.Invoke(prop.PropertyType));
            }
         }
         else
            executorInstance = null;

         //Return resolved instance
         return executorInstance;
      }

      public Type ResolvePayloadType(string typeName)
      {
         if (string.IsNullOrEmpty(typeName))
            return null;

         Type type;
         if (!mPayloadTypes.TryGetValue(typeName, out type))
            type = null;

         return type;
      }

      public IEnumerable<Type> DetectedPayloadTypes => mPayloadTypes.Values;
   }
}
