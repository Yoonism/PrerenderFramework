// float _DirectionalLightIntensity;
// float _SurfaceDiffuseIntensity;
// float _SkyLightIntensity;

// Screen Probe Reprojection //
#define REPROJECTION_AREA_SEARCH 1
#define REPROJECTION_WITH_RELAXED_WEIGHTS 1

// Spatial Reservoir Prepass //
#define DISK_FILTER_USE_AO_GUIDED_RADIUS 1
#define DISK_FILTER_MIN_RADIUS 0.35
#define DISK_FILTER_STEP_SIZE 0.15
#define DISK_FILTER_MAX_SEARCH_SAMPLES 32
#define DISK_FILTER_MINIMUM_ACCEPT_WEIGHT 0.05

// Probe Ambient Occlusion
#define PROBE_AO_CLAMP_DISTANCE 0.5
#define PROBE_AO_MAX_TEMPORAL_SAMPLES 8

// ReSTIR settings //
#define RESTIR_EXPOSURE_CONTROL 1
#define RESTIR_MAX_HISTORY 20

// Occlusion Validation //
#define MAX_OCCLUSION_TRACING_DISTANCE 5
#define MAX_OCCLUSION_TEMPORAL_SAMPLES 16

// Screen Probe Interpolation
#define USE_RESERVOIR_RAY_DIRECTION 0
#define INTERPOLATION_NORMAL_BOOST 1
#define INTERPOLATION_SAMPLES 9

// Denoiser
#define TEMPORAL_DENOISER_EXPOSURE_CONTROL 1
#define USE_NORMAL_REJECTION 1
#define AABB_CLIP_EXTENT 4
#define MAX_SAMPLECOUNT 32

// Radiance Cache
#define FREEZE_CACHE 0
#define ADAPTIVE_TEMPORAL_WEIGHT 1
#define MAX_TEMPORAL_WEIGHT 0.85
#define MIN_TEMPORAL_WEIGHT 0.30

// Sky Occlusion //
#define EVALUATE_SKY_OCCLUSION 1
#define MINIMAL_SKY_LIGHTING 0

// Reflection Probes
#define OCCLUSION_CHECK 1    // Helps to mitigate light leaks
#define SPATIAL_SAMPLES 25   // 1 - no filtering, 9 - 3x3 filter, 25 - 5x5 filter
#define JITTER_STRENGTH 25   // 0 - no jitter, 9 - 3x3 jitter, 25 - 5x5 jitter
#define JITTER_TEMPORAL 1    // 1 - enable temporal noise

// Indirect Intensity Multipliers (values higher than 1.0 are NOT physically correct!)
#define DIRECTIONAL_LIGHT_INTENSITY 1
#define SURFACE_DIFFUSE_INTENSITY 1
#define SKY_LIGHT_INTENSITY 1

// Fallback
#define APV_FALLBACK 1

// Disable Features
#define DISABLE_WS_TRACING 0
#define DISABLE_SS_TRACING 0
#define DISABLE_PROBE_JITTER 0 
#define DISABLE_RESTIR_TEMPORAL 0
#define DISABLE_RESTIR_SPATIAL 0
#define DISABLE_INTERPOLATION 0
#define DISABLE_TEMPORAL_DENOISER 0