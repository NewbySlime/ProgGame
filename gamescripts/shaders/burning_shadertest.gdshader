shader_type canvas_item;

uniform sampler2D noiseTex;

uniform vec4 colMax: hint_color;
uniform vec4 colMin: hint_color;

uniform vec4 MmxUMmxD;
uniform vec4 MmyUMmyD;

uniform vec2 uvOffset;

uniform float maxNoise;
uniform int stepNoise;

float getAlpha(vec2 maxmin, float pos, float currentAlpha, bool upside){
	float res;
	if(upside)
		res = currentAlpha*(1f-(min(pos, maxmin.x)-maxmin.y)/(maxmin.x-maxmin.y));
	else
		res = currentAlpha*((min(max(pos, maxmin.y), maxmin.x)-maxmin.y)/(maxmin.x-maxmin.y));
		
	return res;
}

void fragment(){
	// might need some optimization
	
	vec4 deltacol = colMax - colMin;
	vec2 curruv = UV+uvOffset;
	deltacol = vec4(deltacol.xyz * UV.y, 1f);
	
	COLOR = vec4((deltacol + colMin).xyz, texture(noiseTex, vec2(curruv.x, curruv.y + TIME)).x < maxNoise? 0f: 1f);

	if(UV.x > MmxUMmxD.y)
		COLOR.a = getAlpha(MmxUMmxD.xy, UV.x, COLOR.a, true);
	else if(UV.x < MmxUMmxD.w)
		COLOR.a = getAlpha(MmxUMmxD.wz, UV.x, COLOR.a, false);
	
	if(UV.y > MmyUMmyD.y)
		COLOR.a = getAlpha(MmyUMmyD.xy, UV.y, COLOR.a, true);
	else if(UV.y < MmyUMmyD.w)
		COLOR.a = getAlpha(MmyUMmyD.wz, UV.y, COLOR.a, false);
	
}