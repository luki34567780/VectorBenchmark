using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace VectorBenchmark
{
    [BenchmarkDotNet.Attributes.SimpleJob]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public unsafe class VectorBenchmark
    {
        public const int ItemsCount = (int)(1024 * 1024 * 1024 * 1.999999);
        public int[] Data { get; set; }
        public int* DataPtr { get; set; }
        public GCHandle Handle { get; set; }

        [IterationSetup]
        public void Setup()
        {
            Data = new int[ItemsCount];
            Handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            DataPtr = (int*)Handle.AddrOfPinnedObject();
        }

        [IterationCleanup]
        public void Cleanup()
        {
            Handle.Free();
        }

        [Benchmark]
        public void DynamicSizeVector()
        {
            int vectorCount = ItemsCount / Vector<int>.Count;
            int itemsInVector = Vector<int>.Count;

            for (int i = 0; i < vectorCount; i++)
            {
                int index = i * itemsInVector;
                var src = new Vector<int>(Data, index);
                Vector.Add(src, src).CopyTo(Data, index);
            }
        }

        [Benchmark]
        public void VectorSize256()
        {
            int vectorCount = ItemsCount / Vector256<int>.Count;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;
            for (int i = 0; i < vectorCount; i++)
            {
                var src = Avx2.LoadVector256(ptr);
                var result = Avx2.Add(src, src);
                Avx2.Store(ptr, result);

                ptr += 8;
            }

            while (ptr != end)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }
        }

        [Benchmark]
        public void VectorSize128()
        {
            int vectorCount = ItemsCount / Vector128<int>.Count;
            int itemsInVector = Vector128<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;
            for (int i = 0; i < vectorCount; i++)
            {
                var src = Avx2.LoadVector128(ptr);
                var result = Avx2.Add(src, src);
                Avx2.Store(ptr, result);

                ptr += 4;
            }

            while (ptr != end)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }
        }

        [Benchmark]
        public void VectorSize256Reordered()
        {
            int vectorCount = (ItemsCount / Vector256<int>.Count) - 1;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            var src = Avx2.LoadVector256(ptr);

            for (int i = 0; i < vectorCount; i++)
            {
                var result = Avx2.Add(src, src);
                Avx2.Store(ptr, result);

                ptr += 8;

                src = Avx2.LoadVector256(ptr);
            }

            while (ptr != end)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }
        }

        [Benchmark]
        public void VectorSize128Reordered()
        {
            int vectorCount = (ItemsCount / Vector128<int>.Count) - 1;
            int itemsInVector = Vector128<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            var src = Avx2.LoadVector128(ptr);

            for (int i = 0; i < vectorCount; i++)
            {
                var result = Avx2.Add(src, src);
                Avx2.Store(ptr, result);

                ptr += 4;

                src = Avx2.LoadVector128(ptr);
            }

            while (ptr != end)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }
        }

        [Benchmark]
        public void VectorSize256Alligned()
        {
            const long Alignment = 256 / 8;
            int vectorCount = (ItemsCount / Vector256<int>.Count) - 1;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            int* ptrAlligned = (int*)(((long)ptr + Alignment + 1) & (long)-(int)Alignment);

            uint unalligned = (uint)(ptrAlligned - ptr);

            for (int i = 0; i < unalligned; i++)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }

            for (int i = 0; i < vectorCount; i++)
            {
                var src = Avx2.LoadAlignedVector256(ptrAlligned);
                var result = Avx2.Add(src, src);
                Avx2.StoreAligned(ptrAlligned, result);

                ptrAlligned += 8;
            }

            while (ptrAlligned++ != end)
            {
                *ptrAlligned = *ptrAlligned + *ptrAlligned;
            }
        }

        [Benchmark]
        public void VectorSize256ReorderedAlligned()
        {
            const long Alignment = 256 / 8;
            int vectorCount = (ItemsCount / Vector256<int>.Count) - 2;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            int* ptrAlligned = (int*)(((long)ptr + Alignment + 1) & (long)-(int)Alignment);

            uint unalligned = (uint)(ptrAlligned - ptr);

            for (int i = 0; i < unalligned; i++)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }
            var src = Avx2.LoadAlignedVector256(ptrAlligned);

            for (int i = 0; i < vectorCount; i++)
            {
                var result = Avx2.Add(src, src);
                Avx2.StoreAligned(ptrAlligned, result);

                ptrAlligned += 8;

                src = Avx2.LoadAlignedVector256(ptrAlligned);
            }

            while (ptrAlligned++ != end)
            {
                *ptrAlligned = *ptrAlligned + *ptrAlligned;
            }
        }

        [Benchmark]
        public void VectorSize256ReorderedAllignedV2()
        {
            const long Alignment = 256 / 8;
            int vectorCount = (ItemsCount / Vector256<int>.Count) - 2;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            int* ptrAlligned = (int*)(((long)ptr + Alignment + 1) & (long)-(int)Alignment);

            var src = Avx2.LoadAlignedVector256(ptrAlligned);

            uint unalligned = (uint)(ptrAlligned - ptr);

            for (int i = 0; i < unalligned; i++)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }

            for (int i = 0; i < vectorCount; i++)
            {
                var result = Avx2.Add(src, src);

                src = Avx2.LoadAlignedVector256(ptrAlligned);
                Avx2.StoreAligned(ptrAlligned, result);

                ptrAlligned += 8;
            }

            while (ptrAlligned++ != end)
            {
                *ptrAlligned = *ptrAlligned + *ptrAlligned;
            }
        }

        [Benchmark]
        public void VectorSize256X4ReorderedAlligned()
        {
            const long Alignment = 1024 / 8;
            int vectorCount = (ItemsCount / Vector256<int>.Count / 4) - 2;
            int itemsInVector = Vector256<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            int* ptrAligned = (int*)(((long)ptr + Alignment + 1) & (long)-(int)Alignment);
            int* ptrAlignedVec2 = ptrAligned + 8;
            int* ptrAlignedVec3 = ptrAligned + 16;
            int* ptrAlignedVec4 = ptrAligned + 24;
            var vec1 = Avx2.LoadAlignedVector256(ptrAligned);
            var vec2 = Avx2.LoadAlignedVector256(ptrAligned + 8);
            var vec3 = Avx2.LoadAlignedVector256(ptrAligned + 16);
            var vec4 = Avx2.LoadAlignedVector256(ptrAligned + 24);

            uint unalligned = (uint)(ptrAligned - ptr);

            for (int i = 0; i < unalligned; i++)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }

            for (int i = 0; i < vectorCount; i++)
            {
                var result1 = Avx2.Add(vec1, vec1);
                var result2 = Avx2.Add(vec2, vec2);
                var result3 = Avx2.Add(vec3, vec3);
                var result4 = Avx2.Add(vec4, vec4);

                vec1 = Avx2.LoadAlignedVector256(ptrAligned);
                Avx2.StoreAligned(ptrAligned, result1);
                vec2 = Avx2.LoadAlignedVector256(ptrAlignedVec2);
                Avx2.StoreAligned(ptrAlignedVec2, result2);
                vec3 = Avx2.LoadAlignedVector256(ptrAlignedVec3);
                Avx2.StoreAligned(ptrAlignedVec3, result3);
                vec4 = Avx2.LoadAlignedVector256(ptrAlignedVec4);
                Avx2.StoreAligned(ptrAlignedVec4, result4);

                ptrAligned += 32;
                ptrAlignedVec2 += 32;
                ptrAlignedVec3 += 32;
                ptrAlignedVec4 += 32;
            }

            while (ptrAlignedVec4++ != end)
            {
                *ptrAlignedVec4 = *ptrAlignedVec4 + *ptrAlignedVec4;
            }
        }

        [Benchmark]
        public void VectorSize128ReorderedAlligned()
        {
            const long Alignment = 128 / 8;
            int vectorCount = (ItemsCount / Vector128<int>.Count) - 2;
            int itemsInVector = Vector128<int>.Count;

            int* ptr = DataPtr;
            int* end = DataPtr + ItemsCount;

            int* ptrAlligned = (int*)(((long)ptr + Alignment + 1) & (long)-(int)Alignment);

            uint unalligned = (uint)(ptrAlligned - ptr);

            for (int i = 0; i < unalligned; i++)
            {
                *ptr = *ptr + *ptr;
                ptr++;
            }

            var src = Avx2.LoadAlignedVector128(ptrAlligned);

            for (int i = 0; i < vectorCount; i++)
            {
                var result = Avx2.Add(src, src);
                Avx2.StoreAligned(ptrAlligned, result);
                src = Avx2.LoadAlignedVector128(ptrAlligned);

                ptrAlligned += 4;
            }

            while (ptrAlligned++ != end)
            {
                *ptrAlligned = *ptrAlligned + *ptrAlligned;
            }
        }

        //[Benchmark]
        //public void VectorSize512()
        //{
        //    int vectorCount = ByteCount / Vector512<int>.Count;
        //    int itemsInVector = Vector512<int>.Count;

        //    int* ptr = DataPtr;
        //    for (int i = 0; i < vectorCount; i++)
        //    {
        //        Vector512<int> src = Avx512F.LoadVector512(ptr);
        //        var result = Avx512F.Add(src, src);
        //        Avx512F.Store(ptr, result);

        //        ptr += 256 / 8;
        //    }
        //}

        [Benchmark]
        public void LINQ()
        {
            Data = Data.Select(x => x + x).ToArray();
        }

        [Benchmark]
        public void Traditional()
        {
            for (int i = 0; i < ItemsCount; i++) 
            {
                int val = Data[i];
                Data[i] = (val + val);
            }
        }

        [Benchmark]
        public void RawPointers()
        {
            int* end = DataPtr + ItemsCount;
            int* ptr = DataPtr;

            while (ptr++ != end) 
            {
                var val = *ptr;
                *ptr = val + val;
            }
        }
    }
}
