using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventVisualizer.Base {

	public static class TypesHelper {
		
		public static void GetTypesMatching<T>(HashSet<Type> list) {
#if NET_4_6
			var types = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
				.SelectMany(a => a.GetTypes())
				.Where(t => typeof(T).IsAssignableFrom(t));
#else
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t => typeof(Component).IsAssignableFrom(t));
#endif

			foreach (var type in types) {
				list.Add(type);					
			}
		}
		
		public static HashSet<Type> GetTypesMatching<T>() {
			HashSet<Type> set = new HashSet<Type>();
			GetTypesMatching<T>(set);
			return set;
		}

		/// <summary>
		/// Search for types that have a field or property of type <typeparamref name="T"/> or can hold an object that can.
		/// </summary>
		/// <typeparam name="T">Needle</typeparam>
		/// <param name="containerType">Haystack</param>
		/// <returns>Can contain some object <typeparamref name="T"/></returns>
		public static bool RecursivelyFindFieldsOfType<TT>(Type containerType, Dictionary<Type, bool> matchingTypes) {
			bool wanted;
			
			// bail if the type was already explored
			if (matchingTypes.TryGetValue(containerType, out wanted)) return wanted;

			// the type was not seen yet
			matchingTypes.Add(containerType, false);

			var targetType = typeof(TT);
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var query = containerType.GetFields(flags)
				.Where(f => !f.FieldType.IsPrimitive)
				.Select(f => f.FieldType)
				.Concat(containerType.GetProperties(flags)
					.Select(p => p.PropertyType));
			foreach (var fType in query) {
				if (targetType.IsAssignableFrom(fType)) {
					// This is a shallow discovery, bail on first find
					return matchingTypes[containerType] |= true;
				}
				else if (typeof(UnityEngine.Object).IsAssignableFrom(fType)) {
					continue;
				}
				else if (!matchingTypes.TryGetValue(fType, out wanted)) {
					if (RecursivelyFindFieldsOfType<TT>(fType, matchingTypes)) {
						return matchingTypes[containerType] |= true;
					}
				}
				else if (wanted) {
					return matchingTypes[containerType] |= true;
				}
			}

			if (containerType.IsArray) {
				if (RecursivelyFindFieldsOfType<TT>(containerType.GetElementType(), matchingTypes)) {
					return matchingTypes[containerType] |= true;
				}
			}

			return false;
		}
		
		
	}

}