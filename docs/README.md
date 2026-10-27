# Documentation

This directory contains the technical documentation for AutoValentia.

- [Analysis rules](analysis-rules.md): input requirements, normalization, trial filtering,
  protocol-specific rules, block calculations, and output behavior.

The analysis rules document is the source of truth for the processing behavior. Update it and
the relevant automated tests whenever a rule changes.

## Code map

- `src/Data/MatFileReader.cs` reads and normalizes MATLAB matrices.
- `src/Analysis/LatencyReview.cs` selects trials and builds per-rat blocks.
- `src/Analysis/LatencyReviewSet.cs` applies protocol-level rules.
- `src/Analysis/GraphAnalysis.cs` calculates daily means and SEM for plots.
- `src/Export/ReviewExcelWriter.cs` writes the review workbook.
- `test/Analysis` and `test/Export` contain executable examples of the rules.
