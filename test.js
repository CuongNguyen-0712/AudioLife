import https from "k6/http";
import { sleep } from "k6";

export default function () {
  https.get("https://aorta-sank-surviving.ngrok-free.dev/api/mobile/history");
  sleep(1);
}

// k6 run --vus 50 --duration 10s test.js
// k6 run --vus 250 --duration 10s test.js
// k6 run --vus 1000 --duration 10s test.js
// k6 run --vus 50 --duration 10s test.js


// winget install k6