# [spatialContextResolver/spatial_context.py]
# Role:
#   Normalizes client embodiment signals (gaze/head) into spatial context fields.
#
# Input:
#   - client_data from client TargetData payload
#   - module_spec_path
#
# Output:
#   - updated spatialContextResolver section in module-spec state
#
# Pipeline:
#   client embodiment payload -> spatial_context -> moduleSpec -> projector
#
# Public-release note:
#   Internal implementation is omitted in this public release.


def update_module_spec_spatial(client_data, module_spec_path):
    raise NotImplementedError