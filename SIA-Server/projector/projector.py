from projector.decision_tree import decision_tree_classifier
from projector.context_summary import build_context_summary
from projector.data_summary import build_data_summary

# [projector/projector.py]
# Role:
#   Orchestrates projector-stage composition by merging decision, context_summary,
#   and data_summary into one projection_result.
#
# Input:
#   - emb_result
#   - calibrated_unc_result
#   - processed_text
#   - module_spec_path
#   - chart_spec_path
#
# Output:
#   - projection_result for feedforwardGenerator and dataComposer
#
# Pipeline:
#   decision_tree + context_summary + data_summary -> projector -> downstream modules
#
# Public-release note:
#   Internal implementation is omitted in this public release.


def update_module_spec_projector(
    emb_result,
    calibrated_unc_result,
    processed_text,
    module_spec_path,
    chart_spec_path,
):
    raise NotImplementedError
