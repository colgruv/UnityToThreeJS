var EmissionVolume =
{
	SPHERE : 0,
	CUBE : 1,
	CONE : 2,
	CYLINDER : 3
}

function ParticleSystemData()
{
	// Management
	this.poolSize = 1000;
	this.lifespan = 5000; // (milliseconds)
	
	// Emission Properties
	this.emissionVolume = EmissionVolume.SPHERE;
	this.emissionRate = 20;
	this.radius = 5;
	
	// Particle Color
	this.texURL = "textures/sprites/spark1.png";
	this.useColorDelta = false;
	this.color0 = {r: 1, g: 1, b: 1};
	this.color1 = {r: 0, g: 0, b: 0};
	
	// Particle Size
	this.useSizeDelta = false;
	this.minSize0 = 10; 
	this.maxSize0 = 50;
	this.minSize1 = 1;
	this.maxSize1 = 5;
	
	// Particle Motion
	this.speed0 = 30;
	this.useGravity = false;
	this.gravity = {x: 0, y: -9.8, z: 0};
	
}

function ParticleSystem(_data)
{
	this.poolSize = _data.poolSize;
	this.lifespan = _data.lifespan;
	
	this.radius = _data.radius;
	this.emissionVolume = _data.emissionVolume;
	this.emissionRate = _data.emissionRate;
	this.tEmission = 0;
	
	this.useColorDelta = _data.useColorDelta;
	this.color0 = _data.color0;
	this.color1 = _data.color1;
	
	this.useSizeDelta = _data.useSizeDelta;
	this.minSize0 = _data.minSize0;
	this.maxSize0 = _data.maxSize0;
	this.minSize1 = _data.minSize1;
	this.maxSize1 = _data.maxSize1;
	
	this.speed0 = _data.speed0;
	this.useGravity = _data.useGravity;
	this.gravity = _data.gravity;
	
	var uniforms = 
	{
		color:     { value: new THREE.Color( this.color0 ) },
		texture:   { value: new THREE.TextureLoader().load(_data.texURL) }
	};
	
	var shaderMaterial = new THREE.ShaderMaterial( 
	{
		uniforms:       uniforms,
		vertexShader:   document.getElementById( 'vertexshader' ).textContent,
		fragmentShader: document.getElementById( 'fragmentshader' ).textContent,
		blending:       THREE.AdditiveBlending,
		depthTest:      false,
		transparent:    true
	});
	
	this.geometry = new THREE.BufferGeometry();

	this.positions = new Float32Array(this.poolSize * 3);
	this.velocities = new Float32Array(this.poolSize * 3);
	this.colors = new Float32Array(this.poolSize * 3);
	this.sizes = new Float32Array(this.poolSize);
	this.startSizes = new Float32Array(this.poolSize);
	this.endSizes = new Float32Array(this.poolSize);
	this.alives = new Uint8Array(this.poolSize);
	this.durations = new Float32Array(this.poolSize);

	for ( var i = 0, i3 = 0; i < this.poolSize; i ++, i3 += 3 ) 
	{
		// Particles are not alive by default
		this.alives[i] = 0;
		this.colors[i3 + 0] = 0.0;
		this.colors[i3 + 1] = 0.0;
		this.colors[i3 + 2] = 0.0;
		this.startSizes[i] = 0.0;
		this.endSizes[i] = 0.0;
		this.sizes[i] = 0.0;
	}

	this.geometry.addAttribute( 'position', new THREE.BufferAttribute( this.positions, 3 ) );
	this.geometry.addAttribute( 'customColor', new THREE.BufferAttribute( this.colors, 3 ) );
	this.geometry.addAttribute( 'size', new THREE.BufferAttribute( this.sizes, 1 ) );
	
	this.particles = new THREE.Points(this.geometry, shaderMaterial);
}

function normalize3()
{
	var magnitude = Math.sqrt((this.x*this.x) + (this.y*this.y) + (this.z*this.z));
	this.x /= magnitude;
	this.y /= magnitude;
	this.z /= magnitude;
}

function multiply3(_scalar)
{
	this.x *= _scalar;
	this.y *= _scalar;
	this.z *= _scalar;
}

function generateParticle()
{
	// Calculate the sum of active particles and check to make sure we have additional particles to generate
	var sumAlive = 0;
	for (var i = 0; i < this.poolSize; i++)
	{
		sumAlive += this.alives[i];
	}
	if (sumAlive == this.poolSize)
	{
		//console.log("Cannot activate additional particles. Increase pool size or decrease emission rate.");
		return;
	}
	//console.log("Particles alive: " + sumAlive);
	
	for ( var i = 0, i3 = 0; i < this.poolSize; i ++, i3 += 3 ) 
	{
		// Find a particle in the pool that is not activated
		if (this.alives[i] == 0)
		{
			//console.log("Activating particle at index " + i);
			this.alives[i] = 1;
			this.durations[i] = 0;
			
			// Set color and size to starting values
			this.colors[i3 + 0] = this.color0.r;
			this.colors[i3 + 1] = this.color0.g;
			this.colors[i3 + 2] = this.color0.b;
			this.startSizes[i] = (Math.random() * (this.maxSize0 - this.minSize0)) + this.minSize0;
			this.endSizes[i] = (Math.random() * (this.maxSize1 - this.minSize1)) + this.minSize1;
			this.sizes[i] = this.startSizes[i];
			
			if (this.emissionVolume == EmissionVolume.SPHERE)
			{
				emitFromSphere.call(this, i3);
			}
			else if (this.emissionVolume == EmissionVolume.CONE)
			{
				emitFromCone.call(this, i3);
			}
			
			particleSystem.geometry.attributes.size.needsUpdate = true;
			particleSystem.geometry.attributes.position.needsUpdate = true;
			particleSystem.geometry.attributes.customColor.needsUpdate = true;
			
			//console.log("Particle activated at index " + i);
			return;
		}		
	}
}

function emitFromSphere(_i3)
{
	// Generate a random normalized vector and scale it to the radius of the emission volume
	var v3 = 
	{
		x : Math.random() * 2 - 1,
		y : Math.random() * 2 - 1,
		z : Math.random() * 2 - 1
	};
	normalize3.call(v3);
	multiply3.call(v3, Math.random() * this.radius);
	this.positions[_i3 + 0] = v3.x;
	this.positions[_i3 + 1] = v3.y;
	this.positions[_i3 + 2] = v3.z;
	
	// Set initial velocity per emission volume type and speed0 property
	v3 = 
	{
		x : Math.random() * 2 - 1,
		y : Math.random() * 2 - 1,
		z : Math.random() * 2 - 1
	};
	normalize3.call(v3);
	multiply3.call(v3, Math.random() * this.speed0);
	this.velocities[_i3 + 0] = v3.x;
	this.velocities[_i3 + 1] = v3.y;
	this.velocities[_i3 + 2] = v3.z;
}

function emitFromCone(_i3)
{
	this.positions[_i3 + 0] = 0;
	this.positions[_i3 + 1] = 0;
	this.positions[_i3 + 2] = 0;
	
	var v3 = 
	{
		x : Math.random() * this.radius - (this.radius/2),
		y : 1,
		z : Math.random() * this.radius - (this.radius/2)
	};
	normalize3.call(v3);
	multiply3.call(v3, Math.random() * this.speed0);
	this.velocities[_i3 + 0] = v3.x;
	this.velocities[_i3 + 1] = v3.y;
	this.velocities[_i3 + 2] = v3.z;
}

function updateParticles(_dt)
{
	// Determine when a new particle should be generated (activated)
	this.tEmission += _dt;
	if (this.tEmission >= 1000 / this.emissionRate)
	{
		this.tEmission = 0;
		generateParticle.call(this);
		//console.log("Confirming particle activation at index 0: " + this.alives[0]);
	}
	
	// Update all active particles
	for ( var i = 0, i3 = 0; i < this.poolSize; i ++, i3 += 3 ) 
	{
		if (this.alives[i] == 1)
		{
			// Apply forces to velocities
			if (this.useGravity)
			{
				this.velocities[i3 + 0] += this.gravity.x * (_dt/1000);
				this.velocities[i3 + 1] += this.gravity.y * (_dt/1000);
				this.velocities[i3 + 2] += this.gravity.z * (_dt/1000);
			}
			
			// Apply velocities to positions
			this.positions[i3 + 0] += this.velocities[i3 + 0] * (_dt/1000);
			this.positions[i3 + 1] += this.velocities[i3 + 1] * (_dt/1000);
			this.positions[i3 + 2] += this.velocities[i3 + 2] * (_dt/1000);
			particleSystem.geometry.attributes.position.needsUpdate = true;
			
			var t = this.durations[i] / this.lifespan;
			
			if (this.useColorDelta)
			{
				this.colors[i3 + 0] = (this.color0.r * (1-t)) + (this.color1.r * (t));
				this.colors[i3 + 1] = (this.color0.g * (1-t)) + (this.color1.g * (t));
				this.colors[i3 + 2] = (this.color0.b * (1-t)) + (this.color1.b * (t));
			}
			if (this.useSizeDelta)
			{
				this.sizes[i] = (this.startSizes[i] * (1-t)) + (this.endSizes[i] * (t));
			}
			
			// Track particle duration against lifespan and deactivate particles when applicable
			this.durations[i] += _dt;
			if (this.durations[i] >= this.lifespan)
			{
				this.alives[i] = 0;
				this.sizes[i] = 0;
				this.colors[i3 + 0] = 0.0;
				this.colors[i3 + 1] = 0.0;
				this.colors[i3 + 2] = 0.0;
				
				particleSystem.geometry.attributes.size.needsUpdate = true;
				particleSystem.geometry.attributes.customColor.needsUpdate = true;
			}
		}		
	}
}