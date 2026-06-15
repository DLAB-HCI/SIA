# [projector/data_summary.py]
# Role:
#   Builds data_summary from chart context, saved specs, and sampled dataset clusters.
#
# Input:
#   - chart_spec_path
#   - dataset and clusters metadata
#   - optional latest saved chart-spec snapshots
#
# Output:
#   - data_summary object (axis/domain/clusters/row_fields/row_values/chart context)
#
# Pipeline:
#   chartSpec + dataset -> data_summary -> projector
#
# Public-release note:
#   Internal implementation is omitted in this public release.


def _load_json_if_exists(path):
    raise NotImplementedError


def _safe_eval(value):
    raise NotImplementedError


def _sample_clusters(clusters_df):
    raise NotImplementedError


def _find_latest_saved_spec():
    raise NotImplementedError


def build_data_summary(chart_spec_path):
    raise NotImplementedError