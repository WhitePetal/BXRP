using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace BXRenderPipeline
{
	// Non-allocating sorts
    internal struct Sorting
	{
		public static ProfilingSampler s_QuickSortSampler = new ProfilingSampler("QuickSort");
		public static ProfilingSampler s_InsertionSortSampler = new ProfilingSampler("InsertSort");

		public static void QuickSort<T>(T[] data, Func<T, T, int> compare)
		{
			using var scope = new ProfilingScope(null, s_QuickSortSampler);
			QuickSort<T>(data, 0, data.Length - 1, compare);
		}

		public static void QuickSort<T>(T[] data, int start, int end, Func<T, T, int> compare)
		{
			int diff = end - start;
			if (diff < 1) return;
			if(diff < 8)
			{
				InsertionSort(data, start, end, compare);
				return;
			}

			Assert.IsTrue(start < data.Length);
			Assert.IsTrue(end < data.Length);

			if(start < end)
			{
				int pivot = Partition<T>(data, start, end, compare);

				if (pivot >= 1)
					QuickSort<T>(data, start, pivot, compare);

				if (pivot + 1 < end)
					QuickSort<T>(data, pivot + 1, end, compare);
			}
		}

		private static T Median3Pivot<T>(T[] data, int start, int pivot, int end, Func<T, T, int> compare)
		{
			void Swap(int a, int b)
			{
				var temp = data[a];
				data[a] = data[b];
				data[b] = temp;
			}

			if (compare(data[end], data[start]) < 0) Swap(start, end);
			if (compare(data[pivot], data[start]) < 0) Swap(start, pivot);
			if (compare(data[end], data[pivot]) < 0) Swap(pivot, end);
			return data[pivot];
		}

		private static int Partition<T>(T[] data, int start, int end, Func<T, T, int> compare)
		{
			int diff = end - start;
			int pivot = start + diff / 2;

			var pivotValue = Median3Pivot(data, start, pivot, end, compare);

			while (true)
			{
				while (compare(data[start], pivotValue) < 0) ++start;
				while (compare(data[end], pivotValue) > 0) --end;

				if (start >= end) return end;

				var temp = data[start];
				data[start++] = data[end];
				data[end--] = temp;
			}
		}

		public static void InsertionSort<T>(T[] data, Func<T, T, int> compare)
		{
			using var scope = new ProfilingScope(null, s_InsertionSortSampler);
			InsertionSort<T>(data, 0, data.Length - 1, compare);
		}

		public static void InsertionSort<T>(T[] data, int start, int end, Func<T, T, int> compare)
		{
			Assert.IsTrue(start < data.Length);
			Assert.IsTrue(end < data.Length);

			for(int i = start + 1; i < end + 1; i++)
			{
				var iData = data[i];
				int j = i - 1;
				while(j >= 0 && compare(iData, data[j]) < 0)
				{
					data[j + 1] = data[j];
					j--;
				}
				data[j + 1] = iData;
			}
		}
	}
}
