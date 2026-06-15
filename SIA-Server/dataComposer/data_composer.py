# [dataComposer/data_composer.py]
# Role:
#   Converts projector outputs into a chart-spec generation request and returns
#   the generated chart-spec code payload to the server layer.
#
# Input:
#   - projection_result: context/data summary from projector
#   - chart_spec_path: optional path to current chartSpec.json
#   - module_spec_path: optional path to moduleSpec.json
#   - text/code payload for utility parsing helpers
#
# Output:
#   - (data_composer_result, is_reset) contract for server integration
#   - normalized text payload for downstream parsing
#   - extracted spec dictionary (when available)
#
# Pipeline:
#   projector -> dataComposer -> main_server
#
# Public-release note:
#   Internal implementation is omitted in this public release.


def prepare_llm_inputs(projection_result):
    raise NotImplementedError


def update_chart_spec(chart_spec_path=None, module_spec_path=None, projection_result=None):
    raise NotImplementedError


def strip_md_and_bom(text: str) -> str:
    raise NotImplementedError


def extract_spec_dict_from_text(code_str: str):
    raise NotImplementedError