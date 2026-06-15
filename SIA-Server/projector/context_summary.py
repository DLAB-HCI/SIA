# [projector/context_summary.py]
# Role:
#   Builds context_summary from decision output and module specification state.
#
# Input:
#   - decision (utterance, speech_acts, spatial_context_scope)
#   - module_spec_path
#
# Output:
#   - context_summary object for downstream modules
#
# Pipeline:
#   decision_tree -> context_summary -> projector
#
# Public-release note:
#   Internal implementation is omitted in this public release.


def build_context_summary(decision, module_spec_path):
    raise NotImplementedError