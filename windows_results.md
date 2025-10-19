 Benchmark_ValidationApproaches_ReliableScaling
   Source: PerformanceTests.cs line 292
   Duration: 8.5 sec

  Standard Output: 

==========================================================================================
RELIABLE SCALING BENCHMARK - EventChains vs Traditional
(Including 1M and 10M iterations for enterprise-scale analysis)
==========================================================================================

      Size |      EC Time |    Trad Time |         EC/sec |       Trad/sec |   Overhead
------------------------------------------------------------------------------------------
Testing 1,000 iterations... Done!
      1000 |       1.34ms |       0.03ms |       746547/s |     35842294/s |   4701.1%
Testing 5,000 iterations... Done!
      5000 |       6.81ms |       0.17ms |       734538/s |     28702641/s |   3807.6%
Testing 10,000 iterations... Done!
     10000 |      13.22ms |       0.30ms |       756636/s |     33079722/s |   4271.9%
Testing 50,000 iterations... Done!
     50000 |      52.18ms |       1.14ms |       958262/s |     43921293/s |   4483.4%
Testing 100,000 iterations... Done!
    100000 |      96.86ms |       1.39ms |      1032430/s |     71756602/s |   6850.3%
Testing 1,000,000 iterations... Done!
   1000000 |     685.49ms |      14.92ms |      1458816/s |     67032665/s |   4495.0%
Testing 10,000,000 iterations... Done!
  10000000 |    7036.44ms |     156.87ms |      1421174/s |     63747174/s |   4385.5%
------------------------------------------------------------------------------------------

ABSOLUTE OVERHEAD (EventChain time - Traditional time):
------------------------------------------------------------------------------------------
      1000 iterations: +      1.31ms total | +  0.001312ms per iteration
      5000 iterations: +      6.63ms total | +  0.001327ms per iteration
     10000 iterations: +     12.91ms total | +  0.001291ms per iteration
     50000 iterations: +     51.04ms total | +  0.001021ms per iteration
    100000 iterations: +     95.47ms total | +  0.000955ms per iteration
   1000000 iterations: +    670.57ms total | +  0.000671ms per iteration
  10000000 iterations: +   6879.57ms total | +  0.000688ms per iteration
------------------------------------------------------------------------------------------

OVERHEAD TREND:
------------------------------------------------------------------------------------------
      1000 →       5000: Per-op overhead   1.31μs →   1.33μs (+1.1%) STABLE
      5000 →      10000: Per-op overhead   1.33μs →   1.29μs (-2.6%) STABLE
     10000 →      50000: Per-op overhead   1.29μs →   1.02μs (-21.0%) BETTER
     50000 →     100000: Per-op overhead   1.02μs →   0.95μs (-6.5%) BETTER
    100000 →    1000000: Per-op overhead   0.95μs →   0.67μs (-29.8%) BETTER
   1000000 →   10000000: Per-op overhead   0.67μs →   0.69μs (+2.6%) BETTER
------------------------------------------------------------------------------------------

THROUGHPUT SCALING ANALYSIS:
------------------------------------------------------------------------------------------
  First scale (1,000):       746547 ops/sec, 1.31μs overhead/op
  Last scale (10,000,000):     1421174 ops/sec, 0.69μs overhead/op
  Throughput change:        +90.4%
  Per-op overhead change:   +47.5% BETTER
==========================================================================================

SCALE VALIDATION:
  Small scale (avg first 3): 1.31μs overhead/op
  Large scale (avg last 2):  0.68μs overhead/op
  ✅ CONFIRMED: 48.1% improvement at scale due to CPU optimizations

