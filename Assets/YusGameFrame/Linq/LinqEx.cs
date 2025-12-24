using System;
using System.Collections.Generic;
using System.Linq;

namespace YusGameFrame
{
    /// <summary>
    /// 对 LINQ 的“游戏开发向”扩展方法集合（入口统一在 <see cref="LinqEx"/>）。
    /// </summary>
    public static partial class LinqEx
    {
        /// <summary>
        /// 判断序列是否没有任何元素（<c>null</c> 也视为“没有”）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <returns>当 <paramref name="source"/> 为 <c>null</c> 或为空时返回 <c>true</c>。</returns>
        public static bool None<T>(this IEnumerable<T> source)
            => source == null || !source.Any();

        /// <summary>
        /// 判断序列中是否“没有任何元素满足条件”（<c>null</c> 也视为“没有”）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列；为 <c>null</c> 时直接返回 <c>true</c>。</param>
        /// <param name="predicate">判断条件；当 <paramref name="source"/> 不为 <c>null</c> 时不能为空。</param>
        /// <returns>当没有任何元素满足 <paramref name="predicate"/> 时返回 <c>true</c>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 不为 <c>null</c> 且 <paramref name="predicate"/> 为 <c>null</c>。</exception>
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                return true;
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return !source.Any(predicate);
        }

        /// <summary>
        /// 将 <c>null</c> 序列转换为“空序列”，便于链式调用（避免空引用）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <returns>若 <paramref name="source"/> 为 <c>null</c>，返回空序列；否则返回原序列。</returns>
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
            => source ?? Enumerable.Empty<T>();

        /// <summary>
        /// 判断序列是否为 <c>null</c> 或为空。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <returns>当 <paramref name="source"/> 为 <c>null</c> 或无元素时返回 <c>true</c>。</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
            => source == null || !source.Any();

        /// <summary>
        /// 判断序列的元素数量是否至少为指定数量（最多只枚举 <paramref name="count"/> 个元素）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列；为 <c>null</c> 时返回 <c>false</c>。</param>
        /// <param name="count">最少数量；当 ≤ 0 时返回 <c>true</c>。</param>
        /// <returns>当序列元素数量 ≥ <paramref name="count"/> 时返回 <c>true</c>。</returns>
        public static bool CountAtLeast<T>(this IEnumerable<T> source, int count)
        {
            if (count <= 0)
                return true;
            if (source == null)
                return false;

            using (var enumerator = source.GetEnumerator())
            {
                for (var i = 0; i < count; i++)
                {
                    if (!enumerator.MoveNext())
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 对序列中的每个元素执行一次回调（常用于游戏逻辑中的遍历副作用）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="action">对每个元素执行的操作。</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="action"/> 为 <c>null</c>。</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
                action(item);
        }

        /// <summary>
        /// 条件版 <c>Where</c>：当 <paramref name="when"/> 为 <c>false</c> 时不进行过滤。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列；当 <paramref name="when"/> 为 <c>false</c> 时会原样返回（可能为 <c>null</c>）。</param>
        /// <param name="predicate">过滤条件；当 <paramref name="when"/> 为 <c>true</c> 时会传给 LINQ 的 <c>Where</c>。</param>
        /// <param name="when">是否启用过滤。</param>
        /// <returns>过滤后的序列或原序列。</returns>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate, bool when)
            => when ? source.Where(predicate) : source;

        /// <summary>
        /// 过滤掉引用类型序列中的 <c>null</c> 元素。
        /// </summary>
        /// <typeparam name="T">引用类型元素。</typeparam>
        /// <param name="source">源序列。</param>
        /// <returns>不包含 <c>null</c> 的序列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <c>null</c>。</exception>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            foreach (var item in source)
            {
                if (item != null)
                    yield return item;
            }
        }

        /// <summary>
        /// 过滤掉 Nullable 序列中的空值，并返回非空值的值类型序列。
        /// </summary>
        /// <typeparam name="T">值类型元素。</typeparam>
        /// <param name="source">源序列。</param>
        /// <returns>展开后的值类型序列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <c>null</c>。</exception>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<Nullable<T>> source) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            foreach (var item in source)
            {
                if (item.HasValue)
                    yield return item.Value;
            }
        }

        /// <summary>
        /// 先投影再过滤：将元素映射为引用类型结果，并跳过返回 <c>null</c> 的项。
        /// </summary>
        /// <typeparam name="TSource">源元素类型。</typeparam>
        /// <typeparam name="TResult">投影结果类型（引用类型）。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="selector">投影函数。</param>
        /// <returns>不包含 <c>null</c> 的投影结果序列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="selector"/> 为 <c>null</c>。</exception>
        public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> selector) where TResult : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            foreach (var item in source)
            {
                var projected = selector(item);
                if (projected != null)
                    yield return projected;
            }
        }

        /// <summary>
        /// 安全获取序列第一个元素：序列为 <c>null</c> 或为空时不抛异常，返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="value">成功时返回第一个元素；失败时为默认值。</param>
        /// <returns>成功获取到元素返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryFirst<T>(this IEnumerable<T> source, out T value)
        {
            if (source == null)
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

                value = enumerator.Current;
                return true;
            }
        }

        /// <summary>
        /// 安全获取“第一个满足条件的元素”：找不到或参数无效时返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列；为 <c>null</c> 时直接失败。</param>
        /// <param name="predicate">判断条件；为 <c>null</c> 时直接失败。</param>
        /// <param name="value">成功时返回元素；失败时为默认值。</param>
        /// <returns>找到满足条件的元素返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T value)
        {
            if (source == null || predicate == null)
            {
                value = default;
                return false;
            }

            foreach (var item in source)
            {
                if (!predicate(item))
                    continue;

                value = item;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 安全判断并获取序列的唯一元素：当且仅当“恰好一个元素”时成功。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列；为 <c>null</c> 时失败。</param>
        /// <param name="value">成功时返回唯一元素；失败时为默认值。</param>
        /// <returns>序列恰好一个元素返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TrySingle<T>(this IEnumerable<T> source, out T value)
        {
            if (source == null)
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

                var first = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    value = default;
                    return false;
                }

                value = first;
                return true;
            }
        }

        /// <summary>
        /// 按 key 去重（保留第一次出现的元素）。
        /// </summary>
        /// <typeparam name="TSource">源元素类型。</typeparam>
        /// <typeparam name="TKey">用于去重的 key 类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="keySelector">key 选择器。</param>
        /// <param name="comparer">key 的比较器（可选）。</param>
        /// <returns>去重后的序列（惰性枚举）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="keySelector"/> 为 <c>null</c>。</exception>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var seenKeys = new HashSet<TKey>(comparer);
            foreach (var item in source)
            {
                if (seenKeys.Add(keySelector(item)))
                    yield return item;
            }
        }

        /// <summary>
        /// 构建字典：当出现重复 key 时，保留第一个值（后续重复项会被忽略）。
        /// </summary>
        /// <typeparam name="TSource">源元素类型。</typeparam>
        /// <typeparam name="TKey">字典 key 类型。</typeparam>
        /// <typeparam name="TValue">字典 value 类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="keySelector">key 选择器。</param>
        /// <param name="valueSelector">value 选择器。</param>
        /// <param name="comparer">key 的比较器（可选）。</param>
        /// <returns>构建出的字典。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>、<paramref name="keySelector"/> 或 <paramref name="valueSelector"/> 为 <c>null</c>。</exception>
        public static Dictionary<TKey, TValue> ToDictionaryFirstWins<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            var dict = new Dictionary<TKey, TValue>(comparer);
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (!dict.ContainsKey(key))
                    dict.Add(key, valueSelector(item));
            }

            return dict;
        }

        /// <summary>
        /// 构建字典：当出现重复 key 时，保留最后一个值（后者覆盖前者）。
        /// </summary>
        /// <typeparam name="TSource">源元素类型。</typeparam>
        /// <typeparam name="TKey">字典 key 类型。</typeparam>
        /// <typeparam name="TValue">字典 value 类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="keySelector">key 选择器。</param>
        /// <param name="valueSelector">value 选择器。</param>
        /// <param name="comparer">key 的比较器（可选）。</param>
        /// <returns>构建出的字典。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>、<paramref name="keySelector"/> 或 <paramref name="valueSelector"/> 为 <c>null</c>。</exception>
        public static Dictionary<TKey, TValue> ToDictionaryLastWins<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            var dict = new Dictionary<TKey, TValue>(comparer);
            foreach (var item in source)
                dict[keySelector(item)] = valueSelector(item);

            return dict;
        }

        /// <summary>
        /// 将序列转换为 <see cref="HashSet{T}"/>，便于快速去重与 Contains 查询。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="comparer">元素比较器（可选）。</param>
        /// <returns>构建出的 HashSet。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <c>null</c>。</exception>
        public static HashSet<T> ToHashSetEx<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new HashSet<T>(source, comparer);
        }

        /// <summary>
        /// 返回使 selector 值最小的元素（序列不能为空）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <typeparam name="TValue">用于比较的值类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="selector">用于比较的值选择器。</param>
        /// <param name="comparer">比较器（可选；默认使用 <see cref="Comparer{T}.Default"/>）。</param>
        /// <returns>使 selector 值最小的元素。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="selector"/> 为 <c>null</c>。</exception>
        /// <exception cref="InvalidOperationException">序列为空。</exception>
        public static T ArgMin<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector, IComparer<TValue> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            comparer = comparer ?? Comparer<TValue>.Default;

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements.");

                var bestItem = enumerator.Current;
                var bestValue = selector(bestItem);

                while (enumerator.MoveNext())
                {
                    var currentItem = enumerator.Current;
                    var currentValue = selector(currentItem);

                    if (comparer.Compare(currentValue, bestValue) < 0)
                    {
                        bestItem = currentItem;
                        bestValue = currentValue;
                    }
                }

                return bestItem;
            }
        }

        /// <summary>
        /// 返回使 selector 值最大的元素（序列不能为空）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <typeparam name="TValue">用于比较的值类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="selector">用于比较的值选择器。</param>
        /// <param name="comparer">比较器（可选；默认使用 <see cref="Comparer{T}.Default"/>）。</param>
        /// <returns>使 selector 值最大的元素。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="selector"/> 为 <c>null</c>。</exception>
        /// <exception cref="InvalidOperationException">序列为空。</exception>
        public static T ArgMax<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector, IComparer<TValue> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            comparer = comparer ?? Comparer<TValue>.Default;

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no elements.");

                var bestItem = enumerator.Current;
                var bestValue = selector(bestItem);

                while (enumerator.MoveNext())
                {
                    var currentItem = enumerator.Current;
                    var currentValue = selector(currentItem);

                    if (comparer.Compare(currentValue, bestValue) > 0)
                    {
                        bestItem = currentItem;
                        bestValue = currentValue;
                    }
                }

                return bestItem;
            }
        }

        /// <summary>
        /// 将序列按分隔符拼接为字符串（内部对元素调用 <c>ToString()</c>）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="separator">分隔符。</param>
        /// <returns>拼接结果字符串。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="separator"/> 为 <c>null</c>。</exception>
        public static string JoinToString<T>(this IEnumerable<T> source, string separator)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (separator == null) throw new ArgumentNullException(nameof(separator));

            return string.Join(separator, source.Select(x => x?.ToString()));
        }

        /// <summary>
        /// 从序列开头开始取元素，直到遇到第一个满足条件的元素为止。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="predicate">停止条件。</param>
        /// <param name="includeMatch">是否包含命中的那个元素。</param>
        /// <returns>截断后的序列（惰性枚举）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="predicate"/> 为 <c>null</c>。</exception>
        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> predicate, bool includeMatch = true)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            foreach (var item in source)
            {
                if (predicate(item))
                {
                    if (includeMatch)
                        yield return item;
                    yield break;
                }

                yield return item;
            }
        }

        /// <summary>
        /// “包含式” TakeWhile：会先返回当前元素，然后再判断条件；第一次条件为 false 时停止（因此会包含该元素）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="predicate">继续条件。</param>
        /// <returns>截断后的序列（惰性枚举）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="predicate"/> 为 <c>null</c>。</exception>
        public static IEnumerable<T> TakeWhileInclusive<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            foreach (var item in source)
            {
                yield return item;
                if (!predicate(item))
                    yield break;
            }
        }

        /// <summary>
        /// 使用 <see cref="Random"/> 对列表进行原地洗牌（Fisher–Yates）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">要洗牌的列表。</param>
        /// <param name="rng">随机数生成器（可传入固定 seed 以复现）。</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> 或 <paramref name="rng"/> 为 <c>null</c>。</exception>
        public static void ShuffleInPlace<T>(this IList<T> list, Random rng)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>
        /// 返回一个被打乱顺序的新列表（当 source 本身就是 <see cref="List{T}"/> 时会原地打乱该列表）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="rng">随机数生成器。</param>
        /// <returns>打乱后的列表。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="rng"/> 为 <c>null</c>。</exception>
        public static List<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var list = source as List<T> ?? source.ToList();
            list.ShuffleInPlace(rng);
            return list;
        }

        /// <summary>
        /// 安全随机取一个元素：参数无效或列表为空时返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">候选列表。</param>
        /// <param name="rng">随机数生成器。</param>
        /// <param name="value">成功时返回抽到的元素；失败时为默认值。</param>
        /// <returns>成功抽取返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryPickRandom<T>(this IList<T> list, Random rng, out T value)
        {
            if (list == null || rng == null || list.Count == 0)
            {
                value = default;
                return false;
            }

            value = list[rng.Next(list.Count)];
            return true;
        }

        /// <summary>
        /// 随机取一个元素：列表为空时抛异常（适合逻辑上“必须有元素”的场景）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list">候选列表。</param>
        /// <param name="rng">随机数生成器。</param>
        /// <returns>抽到的元素。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> 或 <paramref name="rng"/> 为 <c>null</c>。</exception>
        /// <exception cref="InvalidOperationException">列表为空。</exception>
        public static T PickRandom<T>(this IList<T> list, Random rng)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (list.Count == 0) throw new InvalidOperationException("Sequence contains no elements.");

            return list[rng.Next(list.Count)];
        }

        /// <summary>
        /// 权重随机抽取：权重非法（负数/NaN/总权重≤0/无穷）或参数无效时返回 <c>false</c>。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">候选序列。</param>
        /// <param name="weightSelector">权重选择器（建议返回非负有限值）。</param>
        /// <param name="rng">随机数生成器。</param>
        /// <param name="value">成功时返回抽到的元素；失败时为默认值。</param>
        /// <returns>成功抽取返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        public static bool TryPickByWeight<T>(
            this IEnumerable<T> source,
            Func<T, float> weightSelector,
            Random rng,
            out T value)
        {
            if (source == null || weightSelector == null || rng == null)
            {
                value = default;
                return false;
            }

            var list = source as IList<T> ?? source.ToList();
            if (list.Count == 0)
            {
                value = default;
                return false;
            }

            var total = 0f;
            for (var i = 0; i < list.Count; i++)
            {
                var w = weightSelector(list[i]);
                if (w < 0f || float.IsNaN(w))
                {
                    value = default;
                    return false;
                }

                total += w;
            }

            if (total <= 0f || float.IsInfinity(total))
            {
                value = default;
                return false;
            }

            var roll = (float)rng.NextDouble() * total;
            for (var i = 0; i < list.Count; i++)
            {
                roll -= weightSelector(list[i]);
                if (roll <= 0f)
                {
                    value = list[i];
                    return true;
                }
            }

            value = list[list.Count - 1];
            return true;
        }

        /// <summary>
        /// 权重随机抽取：失败时抛异常（适合“掉落表必须能抽出来”的强约束场景）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">候选序列。</param>
        /// <param name="weightSelector">权重选择器。</param>
        /// <param name="rng">随机数生成器。</param>
        /// <returns>抽到的元素。</returns>
        /// <exception cref="InvalidOperationException">无法抽取（参数无效、序列为空或权重非法）。</exception>
        public static T PickByWeight<T>(this IEnumerable<T> source, Func<T, float> weightSelector, Random rng)
        {
            if (!source.TryPickByWeight(weightSelector, rng, out var value))
                throw new InvalidOperationException("Unable to pick a weighted random element.");

            return value;
        }

        /// <summary>
        /// 将序列按固定大小分批（每批返回一个 <see cref="List{T}"/>）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">源序列。</param>
        /// <param name="batchSize">每批大小（必须 &gt; 0）。</param>
        /// <returns>分批后的序列（惰性枚举）。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <c>null</c>。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="batchSize"/> ≤ 0。</exception>
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "batchSize must be > 0.");

            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count < batchSize)
                    continue;

                yield return batch;
                batch = new List<T>(batchSize);
            }

            if (batch.Count > 0)
                yield return batch;
        }
    }
}
