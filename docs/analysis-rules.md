# Analysis rules

This document defines the current analysis behavior implemented by AutoValentia. It is intended
to make the processing reproducible and to provide a concise reference when changing or
rebuilding the application.

## Scope

The application reads behavioral-task data from MATLAB `.mat` files, organizes latencies by day
and rat, and produces an Excel review workbook and latency plots. The same processed data are
used for both outputs.

## Input requirements

### File names

Each file name must contain a day and rat identifier in the form `d<day>r<rat>`, for example:

```text
exp_0526_dis_d3r4.mat
```

Day values may be decimal and both values must be at least 1. A data set cannot contain two files
with the same day and rat identifier.

### MATLAB variable and matrix shape

Each `.mat` file must contain a numeric matrix named `Resultados`. Only matrices with 8 or 9
columns are accepted.

| Input matrix | Processing |
| --- | --- |
| 8 columns | Used without structural changes. |
| 9 columns | Column 9 is removed, together with rows whose value in that column is `2`. Column 1 is then renumbered. |

The original MATLAB file is never changed.

## Trial selection and protocol detection

The following rules apply before block calculations:

1. A row is eligible only when column 8 is greater than `1`.
2. Latency is read from column 4.
3. Column 3 identifies the crossing condition: `0` is a no-conflict crossing and `1` is a conflict crossing.
4. Rows with another column-3 value are not added to either latency condition.

The uploaded set is classified from the eligible rows across all files:

| Eligible conditions present | Protocol type |
| --- | --- |
| No-conflict only | No-conflict crossings |
| Conflict only | Conflict crossings |
| Both conditions | Discrimination |

If no eligible no-conflict or conflict trials exist, analysis stops with an error.

## Three chronological blocks

No-conflict and conflict latencies are divided independently, in their original row order, into
three chronological blocks. The partition is as even as possible. Any remainder goes to block 1
first and then to block 2.

| Number of eligible values | Block sizes |
| --- | --- |
| 1 | 1, 0, 0 |
| 2 | 1, 1, 0 |
| 3 | 1, 1, 1 |
| 4 | 2, 1, 1 |
| 5 | 2, 2, 1 |

When a block has no values for a condition, the Excel workbook shows `n/a` and that rat does not
contribute data for that condition and block in the plot.

## Discrimination rules

### Final day

The final discrimination day is the highest day number among the currently uploaded files. It is
not hard-coded as day 10.

For each final-day file, the analysis keeps only the first 7 eligible no-conflict crossings and
the first 3 eligible conflict crossings. Later trials of each condition are not used for latency
blocks, the workbook summaries, or plots.

If only one discrimination day is uploaded, the application displays a warning and treats that
day as the final day.

### Completing missing conflict blocks

This rule applies only to conflict crossings on discrimination days before the final day. It never
applies to no-conflict crossings.

| Eligible conflict latencies | Block result |
| --- | --- |
| 0 | All conflict blocks remain `n/a`. |
| 1 | Block 1 uses the observed latency. The same value is repeated in blocks 2 and 3. |
| 2 | Blocks 1 and 2 use their observed values. Block 3 uses the arithmetic mean of those two values. |
| 3 or more | The ordinary chronological three-block split is used. |

The calculated values are highlighted in yellow in the Excel workbook. They are included in the
corresponding plot calculations so that the conflict series has three blocks when this rule
applies.

## Conflict-crossing rules

Conflict-crossing sessions remove latency outliers before the chronological split. A row is kept
when its column-4 latency is less than or equal to the day limit below.

| Day | Protocol duration | Accepted latency limit |
| --- | ---: | ---: |
| d1 and d2 | 30 s | 45 s |
| d3 | 60 s | 75 s |
| d4 | 90 s | 105 s |
| d5 | 120 s | 135 s |

The accepted limit is the protocol duration plus 15 seconds. Conflict-crossing files support days
d1 through d5 only. A different day produces an analysis error.

After this filter, the missing conflict-block completion rule also applies to conflict-crossing
sessions. It uses the same one-value repetition and two-value mean described above. No-conflict
blocks are never completed artificially.

## Averages, SEM, and display precision

The app keeps the original latency precision while calculating. For every rat and condition, it
calculates a block mean first. Daily plot means are then calculated from the available rat means,
not from a pooled list of individual trials.

The standard error of the mean (SEM) uses the sample standard deviation of the available rat means
divided by the square root of the number of rats. A single rat has a SEM of `0`.

Excel latency values and means are displayed with two decimal places. Plot means are rounded to
two decimals and plot SEM values to three decimals after calculation.

## Implementation and verification

The implementation is separated by responsibility:

- [MAT reader](../src/Data/MatFileReader.cs) validates and normalizes 8- and 9-column matrices.
- [Latency review builder](../src/Analysis/LatencyReview.cs) filters eligible trials, splits
  blocks, and completes missing conflict blocks.
- [Review-set builder](../src/Analysis/LatencyReviewSet.cs) detects the protocol, limits the
  final discrimination day, and filters conflict outliers.
- [Graph builder](../src/Analysis/GraphAnalysis.cs) produces daily means and SEM.
- [Excel writer](../src/Export/ReviewExcelWriter.cs) writes the review workbook.

The regression tests in [`test/Analysis`](../test/Analysis) and
[`test/Export`](../test/Export) verify the rules above. Any processing-rule change should update
this document and the relevant test in the same change.
