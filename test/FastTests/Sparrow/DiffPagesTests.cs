﻿using System;
using Sparrow;
using Sparrow.Utils;
using Xunit;

namespace FastTests.Sparrow
{
    public unsafe class DiffPagesTests : NoDisposalNeeded
    {
        [Fact]
        public void CanComputeNoDifference()
        {

            var fst = new byte[4096];
            var sec = new byte[4096];

            new Random().NextBytes(fst);
            Buffer.BlockCopy(fst, 0, sec, 0, fst.Length);

            fixed (byte* one = fst)
            fixed (byte* two = sec)
            fixed (byte* tmp = new byte[4096])
            {
                var diffPages = new DiffPages
                {
                    Size = 4096,
                    Original = one,
                    Modified = two,
                    Output = tmp,
                };

                diffPages.ComputeDiff();

                Assert.Equal(0, diffPages.OutputSize);
            }
        }

        [Fact]
        public void CanComputeSmallDifference()
        {
            var fst = new byte[4096];
            var sec = new byte[4096];

            new Random().NextBytes(fst);
            Buffer.BlockCopy(fst, 0, sec, 0, fst.Length);

            sec[12] ++;
            sec[433]++;

            fixed (byte* one = fst)
            fixed (byte* two = sec)
            fixed (byte* tmp = new byte[4096])
            {
                var diffPages = new DiffPages
                {
                    Size = 4096,
                    Original = one,
                    Modified = two,
                    Output = tmp,
                };

                diffPages.ComputeDiff();

                Assert.Equal(32, diffPages.OutputSize);
            }
        }

        [Fact]
        public void CanComputeSmallDifference_AndThenApplyit()
        {
            var fst = new byte[4096];
            var sec = new byte[4096];

            new Random().NextBytes(fst);
            Buffer.BlockCopy(fst, 0, sec, 0, fst.Length);

            sec[12]++;
            sec[433]++;

            fixed (byte* one = fst)
            fixed (byte* two = sec)
            fixed (byte* tmp = new byte[4096])
            {
                var diffPages = new DiffPages
                {
                    Size = 4096,
                    Original = one,
                    Modified = two,
                    Output = tmp,
                };

                diffPages.ComputeDiff();

                new DiffApplier
                {
                    Destination = one,
                    Original = two,
                    Diff = tmp,
                    Size = 4096,
                    DiffSize = diffPages.OutputSize
                }.Apply();

                Assert.Equal(0, Memory.Compare(one, two, 4096));
            }
        }

        [Fact]
        public void CompletelyDifferent()
        {
            var fst = new byte[4096];
            var sec = new byte[4096];

            new Random(1).NextBytes(fst);
            new Random(2).NextBytes(sec);

            fixed (byte* one = fst)
            fixed (byte* two = sec)
            fixed (byte* tmp = new byte[4096])
            {
                var diffPages = new DiffPages
                {
                    Size = 4096,
                    Original = one,
                    Modified = two,
                    Output = tmp,
                };

                diffPages.ComputeDiff();

                Assert.False(diffPages.IsDiff);
            }
        }
    }
}