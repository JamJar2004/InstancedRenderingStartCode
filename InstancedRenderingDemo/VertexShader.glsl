#version 330

layout(location = 0) in vec3 a_position;

out vec3 v_position;

uniform mat4 u_transformation;

void main()
{
	gl_Position = u_transformation * vec4(a_position, 1.0);

	v_position = a_position;
}