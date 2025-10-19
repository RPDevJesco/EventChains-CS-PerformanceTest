==========================================================================================
RELIABLE SCALING BENCHMARK - EventChains vs Traditional
(Including 1M and 10M iterations for enterprise-scale analysis)
==========================================================================================

      Size |      EC Time |    Trad Time |         EC/sec |       Trad/sec |   Overhead
------------------------------------------------------------------------------------------
Testing 1,000 iterations... Done!
      1000 |       0.80ms |       0.02ms |      1254076/s |     65789474/s |   5146.1%
Testing 5,000 iterations... Done!
      5000 |       4.17ms |       0.06ms |      1199789/s |     81037277/s |   6654.3%
Testing 10,000 iterations... Done!
     10000 |       8.79ms |       0.13ms |      1137889/s |     79936051/s |   6924.9%
Testing 50,000 iterations... Done!
     50000 |      43.72ms |       0.62ms |      1143764/s |     80282595/s |   6919.2%
Testing 100,000 iterations... Done!
    100000 |      88.02ms |       1.30ms |      1136087/s |     76663600/s |   6648.0%
Testing 1,000,000 iterations... Done!
   1000000 |     510.09ms |      13.37ms |      1960420/s |     74818378/s |   3716.4%
Testing 10,000,000 iterations... Done!
  10000000 |    4849.77ms |     127.79ms |      2061952/s |     78254854/s |   3695.2%
Testing 100,000,000 iterations... Done!
 100000000 |   48832.55ms |    1292.27ms |      2047814/s |     77382916/s |   3678.8%
------------------------------------------------------------------------------------------

ABSOLUTE OVERHEAD (EventChain time - Traditional time):
------------------------------------------------------------------------------------------
      1000 iterations: +      0.78ms total | +  0.000782ms per iteration
      5000 iterations: +      4.11ms total | +  0.000821ms per iteration
     10000 iterations: +      8.66ms total | +  0.000866ms per iteration
     50000 iterations: +     43.09ms total | +  0.000862ms per iteration
    100000 iterations: +     86.72ms total | +  0.000867ms per iteration
   1000000 iterations: +    496.73ms total | +  0.000497ms per iteration
  10000000 iterations: +   4721.99ms total | +  0.000472ms per iteration
 100000000 iterations: +  47540.27ms total | +  0.000475ms per iteration
------------------------------------------------------------------------------------------

OVERHEAD TREND:
------------------------------------------------------------------------------------------
      1000 →       5000: Per-op overhead   0.78μs →   0.82μs (+5.0%) STABLE
      5000 →      10000: Per-op overhead   0.82μs →   0.87μs (+5.5%) WORSE
     10000 →      50000: Per-op overhead   0.87μs →   0.86μs (-0.5%) STABLE
     50000 →     100000: Per-op overhead   0.86μs →   0.87μs (+0.6%) WORSE
    100000 →    1000000: Per-op overhead   0.87μs →   0.50μs (-42.7%) BETTER
   1000000 →   10000000: Per-op overhead   0.50μs →   0.47μs (-4.9%) BETTER
  10000000 →  100000000: Per-op overhead   0.47μs →   0.48μs (+0.7%) BETTER
------------------------------------------------------------------------------------------

THROUGHPUT SCALING ANALYSIS:
------------------------------------------------------------------------------------------
  First scale (1,000):       1254076 ops/sec, 0.78μs overhead/op
  Last scale (100,000,000):     2047814 ops/sec, 0.48μs overhead/op
  Throughput change:        +63.3%
  Per-op overhead change:   +39.2% BETTER
==========================================================================================

SCALE VALIDATION:
  Small scale (avg first 3): 0.82μs overhead/op
  Large scale (avg last 2):  0.47μs overhead/op
  ✅ CONFIRMED: 42.4% improvement at scale due to CPU optimizations
