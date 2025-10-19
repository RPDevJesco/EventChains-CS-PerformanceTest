==========================================================================================
RELIABLE SCALING BENCHMARK - EventChains vs Traditional
(Including 1M and 10M iterations for enterprise-scale analysis)
==========================================================================================
      Size |      EC Time |    Trad Time |         EC/sec |       Trad/sec |   Overhead
------------------------------------------------------------------------------------------
Testing 1,000 iterations... Done!
      1000 |       1.04ms |       0.03ms |       961631/s |     34965035/s |   3536.0%
Testing 5,000 iterations... Done!
      5000 |       5.41ms |       0.11ms |       923958/s |     45248869/s |   4797.3%
Testing 10,000 iterations... Done!
     10000 |      12.69ms |       0.22ms |       788121/s |     45662100/s |   5693.8%
Testing 50,000 iterations... Done!
     50000 |      57.19ms |       1.06ms |       874300/s |     47058824/s |   5282.5%
Testing 100,000 iterations... Done!
    100000 |      73.28ms |       1.89ms |      1364683/s |     52971713/s |   3781.6%
Testing 1,000,000 iterations... Done!
   1000000 |     677.61ms |      16.89ms |      1475769/s |     59222410/s |   3913.0%
Testing 10,000,000 iterations... Done!
  10000000 |    6503.29ms |     157.31ms |      1537683/s |     63568022/s |   4034.0%
Testing 100,000,000 iterations... Done!
 100000000 |   64103.32ms |    1570.03ms |      1559982/s |     63692868/s |   3982.9%
Testing 1,000,000,000 iterations... Done!
1000000000 |  639069.12ms |   15551.26ms |      1564776/s |     64303453/s |   4009.4%
------------------------------------------------------------------------------------------
ABSOLUTE OVERHEAD (EventChain time - Traditional time):
------------------------------------------------------------------------------------------
      1000 iterations: +      1.01ms total | +  0.001011ms per iteration
      5000 iterations: +      5.30ms total | +  0.001060ms per iteration
     10000 iterations: +     12.47ms total | +  0.001247ms per iteration
     50000 iterations: +     56.13ms total | +  0.001123ms per iteration
    100000 iterations: +     71.39ms total | +  0.000714ms per iteration
   1000000 iterations: +    660.73ms total | +  0.000661ms per iteration
  10000000 iterations: +   6345.98ms total | +  0.000635ms per iteration
 100000000 iterations: +  62533.28ms total | +  0.000625ms per iteration
1000000000 iterations: + 623517.86ms total | +  0.000624ms per iteration
------------------------------------------------------------------------------------------
OVERHEAD TREND:
------------------------------------------------------------------------------------------
      1000 →       5000: Per-op overhead   1.01μs →   1.06μs (+4.8%) STABLE
      5000 →      10000: Per-op overhead   1.06μs →   1.25μs (+17.6%) WORSE
     10000 →      50000: Per-op overhead   1.25μs →   1.12μs (-10.0%) STABLE
     50000 →     100000: Per-op overhead   1.12μs →   0.71μs (-36.4%) BETTER
    100000 →    1000000: Per-op overhead   0.71μs →   0.66μs (-7.4%) BETTER
   1000000 →   10000000: Per-op overhead   0.66μs →   0.63μs (-4.0%) BETTER
  10000000 →  100000000: Per-op overhead   0.63μs →   0.63μs (-1.5%) BETTER
 100000000 → 1000000000: Per-op overhead   0.63μs →   0.62μs (-0.3%) BETTER
------------------------------------------------------------------------------------------
THROUGHPUT SCALING ANALYSIS:
------------------------------------------------------------------------------------------
  First scale (1,000):       961631 ops/sec, 1.01μs overhead/op
  Last scale (1,000,000,000):     1564776 ops/sec, 0.62μs overhead/op
  Throughput change:        +62.7%
  Per-op overhead change:   +38.3% BETTER
==========================================================================================
SCALE VALIDATION:
  Small scale (avg first 3): 1.11μs overhead/op
  Large scale (avg last 2):  0.62μs overhead/op
  ✅ CONFIRMED: 43.5% improvement at scale due to CPU optimizations
