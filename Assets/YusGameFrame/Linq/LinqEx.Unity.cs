using System;
using System.Collections.Generic;
using UnityEngine;

namespace YusGameFrame
{
    public static partial class LinqEx
    {
        /// <summary>
        /// 使用 <see cref="UnityEngine.Random"/> 安全随机取一个元素：列表为空/为 null 时返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">候选列表。</param>
        /// <param name="value">成功时返回抽到的元素；失败时为默认值。</param>
        /// <returns>成功抽取返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryPickRandom<T>(this IList<T> list, out T value)
        {
            if (list == null || list.Count == 0)
            {
                value = default;
                return false;
            }

            value = list[UnityEngine.Random.Range(0, list.Count)];
            return true;
        }

        /// <summary>
        /// 使用 <see cref="UnityEngine.Random"/> 随机取一个元素：列表为空时抛异常。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">候选列表。</param>
        /// <returns>抽到的元素。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> 为 <c>null</c>。</exception>
        /// <exception cref="InvalidOperationException">列表为空。</exception>
        public static T PickRandom<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements.");

            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 使用 <see cref="UnityEngine.Random"/> 对列表进行原地洗牌（Fisher–Yates）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">要洗牌的列表。</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> 为 <c>null</c>。</exception>
        public static void ShuffleInPlace<T>(this IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>
        /// 从序列中找出距离指定位置最近的元素（使用平方距离，避免开根号）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列（不能为空且至少一个元素）。</param>
        /// <param name="position">目标位置。</param>
        /// <param name="positionSelector">从元素提取位置的函数。</param>
        /// <returns>最近的那个元素。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="positionSelector"/> 为 <c>null</c>。</exception>
        /// <exception cref="InvalidOperationException">序列为空。</exception>
        public static T ClosestTo<T>(this IEnumerable<T> source, Vector3 position, Func<T, Vector3> positionSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (positionSelector == null) throw new ArgumentNullException(nameof(positionSelector));

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements.");

                var bestItem = enumerator.Current;
                var bestPos = positionSelector(bestItem);
                var bestDistSq = (bestPos - position).sqrMagnitude;

                while (enumerator.MoveNext())
                {
                    var currentItem = enumerator.Current;
                    var currentPos = positionSelector(currentItem);
                    var currentDistSq = (currentPos - position).sqrMagnitude;

                    if (currentDistSq < bestDistSq)
                    {
                        bestItem = currentItem;
                        bestDistSq = currentDistSq;
                    }
                }

                return bestItem;
            }
        }

        /// <summary>
        /// 安全版最近元素查找：序列为空/参数无效时返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="position">目标位置。</param>
        /// <param name="positionSelector">从元素提取位置的函数。</param>
        /// <param name="value">成功时返回最近元素；失败时为默认值。</param>
        /// <returns>找到返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryClosestTo<T>(this IEnumerable<T> source, Vector3 position, Func<T, Vector3> positionSelector, out T value)
        {
            if (source == null || positionSelector == null)
            {
                value = default;
                return false;
            }

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    value = default;
                    return false;
                }

                var bestItem = enumerator.Current;
                var bestPos = positionSelector(bestItem);
                var bestDistSq = (bestPos - position).sqrMagnitude;

                while (enumerator.MoveNext())
                {
                    var currentItem = enumerator.Current;
                    var currentPos = positionSelector(currentItem);
                    var currentDistSq = (currentPos - position).sqrMagnitude;

                    if (currentDistSq < bestDistSq)
                    {
                        bestItem = currentItem;
                        bestDistSq = currentDistSq;
                    }
                }

                value = bestItem;
                return true;
            }
        }

        /// <summary>
        /// 查找最近的 <see cref="Transform"/>（按 <see cref="Transform.position"/>）。
        /// </summary>
        /// <param name="source">Transform 序列。</param>
        /// <param name="position">目标位置。</param>
        /// <returns>最近的 Transform。</returns>
        public static Transform ClosestTo(this IEnumerable<Transform> source, Vector3 position)
            => source.ClosestTo(position, t => t.position);

        /// <summary>
        /// 查找最近的 <see cref="Component"/>（按其 <see cref="Component.transform"/> 的位置）。
        /// </summary>
        /// <param name="source">Component 序列。</param>
        /// <param name="position">目标位置。</param>
        /// <returns>最近的 Component。</returns>
        public static Component ClosestTo(this IEnumerable<Component> source, Vector3 position)
            => source.ClosestTo(position, c => c.transform.position);

        /// <summary>
        /// 查找最近的 <see cref="GameObject"/>（按其 Transform 位置）。
        /// </summary>
        /// <param name="source">GameObject 序列。</param>
        /// <param name="position">目标位置。</param>
        /// <returns>最近的 GameObject。</returns>
        public static GameObject ClosestTo(this IEnumerable<GameObject> source, Vector3 position)
            => source.ClosestTo(position, go => go.transform.position);
    }
}
