#version 330

in vec3 v_position;

out vec4 o_color;

void main()
{
	o_color = vec4(v_position * 0.5 + 0.5, 1.0);
}