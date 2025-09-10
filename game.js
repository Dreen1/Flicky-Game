Console.log("starting Flick Game Develompment");

const canvas = document.getElementbyId('gameCanvas');
const ctx = canvas.getContext('2d');
const feedback = document.getElementbyId('feedback');
let isDrawing = false;
let startPoint = null;
let startTime = null;

// Circle Tartet (Static for now)
const target = {
  x: 400,
  y: 300,
  radius: 30,
  alive: true
};

// Draw every thing
function draw() {
  Ctx.clearRect(0, 0, canvas.width, canvas.height);

  // Draw Tarte if alive
  if (taret.alive) {
    ctx.beginPath();
    ctx.arc(target.x, target.y, target.radius, 0, Math.PI * 2);
    ctx.fillStyle = 'red';
        ctx.fill();
        ctx.strokeStyle = 'darkred';
        ctx.lineWidth = 2;
        ctx.stroke();
    }
}

// Mouse events
canvas.addEventListener('mousedown', (e) => {
    const rect = canvas.getBoundingClientRect();
    startPoint = {
        x: e.clientX - rect.left,
        y: e.clientY - rect.top
    };
    startTime = Date.now();
    isDrawing = true;
    feedback.textContent = '';
});

canvas.addEventListener('mouseup', (e) => {
    if (!isDrawing) return;
    
    const rect = canvas.getBoundingClientRect();
    const endPoint = {
        x: e.clientX - rect.left,
        y: e.clientY - rect.top
    };
    const endTime = Date.now();
    
    // Calculate flick properties
    const duration = endTime - startTime;
    const distance = Math.sqrt(
        Math.pow(endPoint.x - startPoint.x, 2) + 
        Math.pow(endPoint.y - startPoint.y, 2)
    );
    const speed = distance / duration;
    
    // Validate flick
    const MIN_SPEED = 0.5; // pixels per millisecond
    
    if (speed < MIN_SPEED) {
        feedback.textContent = 'TOO SLOW!';
        feedback.className = 'too-slow';
    } else {
        feedback.textContent = 'VALID FLICK!';
        feedback.className = 'valid';
        
        // Draw the flick line
        ctx.beginPath();
        ctx.moveTo(startPoint.x, startPoint.y);
        ctx.lineTo(endPoint.x, endPoint.y);
        ctx.strokeStyle = 'blue';
        ctx.lineWidth = 3;
        ctx.stroke();
        
        // Check collision (basic for now)
        // This is homework for you to improve
    }
    
    isDrawing = false;
});

// Initial draw
draw();
