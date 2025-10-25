// lp_solver_ui.js
// Robust UI + plotting for Numerical / 2D / 3D
// - builds payload matching C# model: ObjectiveCoefficients, OptimizationType, Constraints[{Coefficients,Relation,RightHandSide}]
// - tolerant to server returning PascalCase or camelCase

// -------------------- Utilities --------------------
function getAny(obj, ...keys) {
    // return first defined among keys (case-sensitive keys passed)
    if (!obj) return undefined;
    for (const k of keys) {
        if (obj[k] !== undefined) return obj[k];
    }
    return undefined;
}

function toNumber(x, fallback = 0) {
    const v = Number(x);
    return isNaN(v) ? fallback : v;
}

// -------------------- Input generation --------------------
function generateInputFields() {
    const numVars = Math.max(2, Math.min(5, parseInt(document.getElementById("numVars").value || 2)));
    const numConstraints = Math.max(1, Math.min(10, parseInt(document.getElementById("numConstraints").value || 3)));
    const container = document.getElementById("dataInputArea");
    container.innerHTML = "";

    // objective
    let html = `<h3>Objective function (Z)</h3><div class="input-row">Z = `;
    for (let i = 0; i < numVars; i++) {
        html += `<input id="obj_${i}" type="number" step="any" value="${i === 0 ? 1 : 0}" style="width:80px;"> x${i + 1}`;
        if (i < numVars - 1) html += " + ";
    }
    html += `</div><hr/><h3>Constraints</h3>`;

    for (let r = 0; r < numConstraints; r++) {
        html += `<div class="input-row" style="margin-bottom:8px;">`;
        for (let i = 0; i < numVars; i++) {
            html += `<input id="c_${r}_${i}" type="number" step="any" value="${r === 0 ? 1 : 0}" style="width:80px;"> x${i + 1}`;
            if (i < numVars - 1) html += " + ";
        }
        html += ` <select id="sign_${r}">
                    <option value="<=">&le;</option>
                    <option value="=">=</option>
                    <option value=">=">&ge;</option>
                  </select>
                  <input id="rhs_${r}" type="number" step="any" value="10" style="width:90px;">`;
        html += `</div>`;
    }

    container.innerHTML = html;
}

// -------------------- Build payload (match C#) --------------------
function buildPayload() {
    const numVars = parseInt(document.getElementById("numVars").value);
    const numConstraints = parseInt(document.getElementById("numConstraints").value);
    const objective = [];
    for (let i = 0; i < numVars; i++) {
        objective.push(toNumber(document.getElementById(`obj_${i}`).value, 0));
    }
    const constraints = [];
    for (let r = 0; r < numConstraints; r++) {
        const coeffs = [];
        for (let i = 0; i < numVars; i++) {
            coeffs.push(toNumber(document.getElementById(`c_${r}_${i}`).value, 0));
        }
        const rel = document.getElementById(`sign_${r}`).value;
        const rhs = toNumber(document.getElementById(`rhs_${r}`).value, 0);
        constraints.push({
            Coefficients: coeffs,       // PascalCase — matches C# model
            Relation: rel,
            RightHandSide: rhs
        });
    }

    const opt = document.getElementById("optType").value || "Maximize";

    return {
        OptimizationType: opt,
        ObjectiveCoefficients: objective,
        Constraints: constraints
    };
}

// -------------------- Convex Hull (2D) - Andrew Monotone Chain --------------------
function convexHull2D(points) {
    // points: [[x,y],...]
    if (!points || points.length === 0) return [];
    const pts = points
        .map(p => ({ x: p[0], y: p[1] }))
        .sort((a, b) => a.x === b.x ? a.y - b.y : a.x - b.x);

    function cross(o, a, b) {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }

    const lower = [];
    for (const p of pts) {
        while (lower.length >= 2 && cross(lower[lower.length - 2], lower[lower.length - 1], p) <= 0) lower.pop();
        lower.push(p);
    }
    const upper = [];
    for (let i = pts.length - 1; i >= 0; i--) {
        const p = pts[i];
        while (upper.length >= 2 && cross(upper[upper.length - 2], upper[upper.length - 1], p) <= 0) upper.pop();
        upper.push(p);
    }
    upper.pop();
    lower.pop();
    const hull = lower.concat(upper);
    return hull.map(p => [p.x, p.y]);
}

// -------------------- Utility: normalize server response shapes --------------------
function normalizeFeasiblePoints(response) {
    // server might return FeasibleVertices array of objects with X/Y/Z or arrays like [[x,y,z],...]
    const arr = getAny(response, "FeasibleVertices", "feasibleVertices", "feasiblePoints", "feasible_points");
    if (!arr) return [];
    const out = [];
    for (const p of arr) {
        if (Array.isArray(p)) {
            out.push([toNumber(p[0]), toNumber(p[1]), toNumber(p[2])]);
        } else {
            // keys may be X,Y,Z or x,y,z
            const x = getAny(p, "X", "x");
            const y = getAny(p, "Y", "y");
            const z = getAny(p, "Z", "z");
            out.push([toNumber(x), toNumber(y), toNumber(z)]);
        }
    }
    return out;
}

function normalizeOptimalPoint(response) {
    const p = getAny(response, "OptimalPoint", "optimalPoint", "Optimal", "optimal");
    if (!p) return null;
    if (Array.isArray(p)) return { x: toNumber(p[0]), y: toNumber(p[1]), z: toNumber(p[2]) };
    return { x: toNumber(getAny(p, "X", "x")), y: toNumber(getAny(p, "Y", "y")), z: toNumber(getAny(p, "Z", "z")) };
}

// -------------------- Plot helpers --------------------
function plot2D(response, payload) {
    const pts3 = normalizeFeasiblePoints(response);
    const pts2 = pts3.map(p => [p[0], p[1]]);
    if (pts2.length === 0) {
        Plotly.purge('lpPlot');
        document.getElementById('lpPlot').innerHTML = "<p>No feasible vertices to plot.</p>";
        return;
    }

    const hull = convexHull2D(pts2);
    // polygon coordinates (close loop)
    const polyX = hull.map(p => p[0]);
    const polyY = hull.map(p => p[1]);
    polyX.push(hull[0][0]); polyY.push(hull[0][1]);

    const traces = [];

    // fill polygon
    traces.push({
        x: polyX,
        y: polyY,
        fill: 'toself',
        fillcolor: 'rgba(0,150,200,0.2)',
        line: { color: 'rgba(0,150,200,0.6)' },
        mode: 'lines',
        name: 'Feasible region'
    });

    // feasible points
    traces.push({
        x: pts2.map(p => p[0]),
        y: pts2.map(p => p[1]),
        mode: 'markers',
        marker: { size: 6, color: 'blue' },
        name: 'Feasible points'
    });

    // draw constraint lines from payload (use first two variables)
    const constraints = payload.Constraints || [];
    if (constraints.length > 0) {
        // compute plotting range
        const allX = pts2.map(p => p[0]);
        const allY = pts2.map(p => p[1]);
        let minX = Math.min(...allX), maxX = Math.max(...allX);
        let minY = Math.min(...allY), maxY = Math.max(...allY);
        // expand a bit
        const padX = (maxX - minX) * 0.2 || 1;
        const padY = (maxY - minY) * 0.2 || 1;
        minX -= padX; maxX += padX; minY -= padY; maxY += padY;

        const xs = [minX, maxX];
        constraints.forEach((c, idx) => {
            const coeffs = getAny(c, "Coefficients", "coefficients", "coefficient");
            if (!coeffs || coeffs.length < 2) return;
            const a = toNumber(coeffs[0]), b = toNumber(coeffs[1]);
            const rhs = toNumber(getAny(c, "RightHandSide", "rightHandSide", "righthandside"));

            // line: a*x + b*y = rhs  => y = (rhs - a*x)/b  if b != 0
            if (Math.abs(b) > 1e-9) {
                const ys = xs.map(x => (rhs - a * x) / b);
                traces.push({
                    x: xs, y: ys, mode: 'lines', name: `Constraint ${idx + 1}`
                });
            } else if (Math.abs(a) > 1e-9) {
                // vertical line x = rhs/a
                const xline = rhs / a;
                traces.push({
                    x: [xline, xline],
                    y: [minY, maxY],
                    mode: 'lines',
                    name: `Constraint ${idx + 1}`
                });
            }
        });
    }

    // optimal point
    const opt = normalizeOptimalPoint(response);
    if (opt) {
        traces.push({
            x: [opt.x],
            y: [opt.y],
            mode: 'markers',
            marker: { size: 10, color: 'red' },
            name: 'Optimal'
        });
    }

    const layout = {
        title: "Graphical Solution (2D)",
        xaxis: { title: 'x1' },
        yaxis: { title: 'x2', scaleanchor: 'x' },
        showlegend: true
    };

    Plotly.newPlot('lpPlot', traces, layout);
}

function plot3D(response, payload) {
    const pts = normalizeFeasiblePoints(response);
    if (pts.length === 0) {
        Plotly.purge('lpPlot');
        document.getElementById('lpPlot').innerHTML = "<p>No feasible vertices to plot.</p>";
        return;
    }

    // scatter of feasible points
    const xs = pts.map(p => p[0]); const ys = pts.map(p => p[1]); const zs = pts.map(p => p[2]);

    const traces = [];
    traces.push({
        x: xs, y: ys, z: zs,
        mode: 'markers',
        type: 'scatter3d',
        marker: { size: 4, color: 'blue' },
        name: 'Feasible points'
    });

    // Plot planes for constraints when possible (z = (d - a*x - b*y)/c)
    const constraints = payload.Constraints || [];
    // bounding box for grid
    let minX = Math.min(...xs), maxX = Math.max(...xs);
    let minY = Math.min(...ys), maxY = Math.max(...ys);
    const padX = (maxX - minX) * 0.2 || 1;
    const padY = (maxY - minY) * 0.2 || 1;
    minX -= padX; maxX += padX; minY -= padY; maxY += padY;

    const gridN = 24; // resolution (increase for smoother surfaces)
    const xLin = Array.from({ length: gridN }, (_, i) => minX + (i / (gridN - 1)) * (maxX - minX));
    const yLin = Array.from({ length: gridN }, (_, i) => minY + (i / (gridN - 1)) * (maxY - minY));

    constraints.forEach((c, idx) => {
        const coeffs = getAny(c, "Coefficients", "coefficients", "coefficient");
        if (!coeffs || coeffs.length < 3) return;
        const a = toNumber(coeffs[0]), b = toNumber(coeffs[1]), cc = toNumber(coeffs[2]);
        const rhs = toNumber(getAny(c, "RightHandSide", "rightHandSide", "righthandside"));
        // we only render as z = f(x,y) when cc != 0
        if (Math.abs(cc) < 1e-9) return;

        // build Z grid
        const zGrid = [];
        for (let i = 0; i < xLin.length; i++) {
            const row = [];
            for (let j = 0; j < yLin.length; j++) {
                const x = xLin[i], y = yLin[j];
                const z = (rhs - a * x - b * y) / cc;
                row.push(z);
            }
            zGrid.push(row);
        }

        traces.push({
            x: xLin,
            y: yLin,
            z: zGrid,
            type: 'surface',
            opacity: 0.45,
            hoverinfo: 'none',
            name: `plane ${idx + 1}`
        });
    });

    // optimal point
    const opt = normalizeOptimalPoint(response);
    if (opt) {
        traces.push({
            x: [opt.x], y: [opt.y], z: [opt.z],
            mode: 'markers',
            type: 'scatter3d',
            marker: { color: 'red', size: 8 },
            name: 'Optimal'
        });
    }

    const layout = {
        title: "Graphical Solution (3D)",
        scene: {
            xaxis: { title: 'x1' },
            yaxis: { title: 'x2' },
            zaxis: { title: 'x3' }
        },
        showlegend: true
    };

    Plotly.newPlot('lpPlot', traces, layout);
}

// -------------------- Main send function --------------------
async function sendSolveRequestFromFields() {
    // gather payload
    const payload = buildPayload();
    console.log("Payload:", payload);

    // determine endpoint
    const method = document.getElementById("solveMethod").value;
    let endpoint = "/api/LPPsolver/Numerical";
    if (method === "2D_Graphical") endpoint = "/api/LPPsolver/graphical2D";
    else if (method === "3D_Graphical") endpoint = "/api/LPPsolver/graphical3D";

    // UI reset
    document.getElementById("status").innerText = "Solving...";
    document.getElementById("objectiveValue").innerText = "--";
    document.getElementById("variableValues").innerHTML = "";
    document.getElementById("lpPlot").innerHTML = "";

    try {
        const resp = await fetch(endpoint, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        const txt = await resp.text();
        let data;
        try {
            data = JSON.parse(txt);
        } catch (e) {
            console.error("Invalid JSON from server:", txt);
            throw new Error("Server returned invalid JSON.");
        }

        if (!resp.ok) {
            console.error("Server error response:", data);
            document.getElementById("status").innerText = "Error";
            document.getElementById("jsonResult").textContent = JSON.stringify(data, null, 2);
            alert("Server error: " + (data.error || data.Message || txt));
            return;
        }

        // set UI values (be tolerant to Pascal/camel)
        const status = getAny(data, "Status", "status") || "Solved";
        const objVal = getAny(data, "ObjectiveValue", "objectiveValue");
        document.getElementById("status").innerText = status;
        document.getElementById("objectiveValue").innerText = (objVal !== undefined && objVal !== null) ? Number(objVal).toFixed(4) : "--";
        // variable values (numerical)
        const varVals = getAny(data, "VariableValues", "variableValues", "variables", "VariableValues");
        if (varVals && typeof varVals === "object") {
            let html = "";
            // if it's array-like:
            if (Array.isArray(varVals)) {
                varVals.forEach((v, i) => html += `<p>x${i + 1} = <strong>${Number(v).toFixed(4)}</strong></p>`);
            } else {
                Object.entries(varVals).forEach(([k, v]) => html += `<p>${k} = <strong>${Number(v).toFixed(4)}</strong></p>`);
            }
            document.getElementById("variableValues").innerHTML = html;
        }

        // raw response
        document.getElementById("jsonResult").textContent = JSON.stringify(data, null, 2);

        // plotting
        if (method === "2D_Graphical") {
            plot2D(data, payload);
        } else if (method === "3D_Graphical") {
            plot3D(data, payload);
        } else {
            Plotly.purge('lpPlot');
            document.getElementById('lpPlot').innerHTML = "<p>Numeric solution - no plot.</p>";
        }

    } catch (err) {
        console.error("Request/processing error:", err);
        document.getElementById("status").innerText = "Error";
        document.getElementById("jsonResult").textContent = err.message || String(err);
        alert("Client error: " + (err.message || err));
    }
}

// -------------------- init --------------------
document.addEventListener('DOMContentLoaded', () => {
    generateInputFields();
    // wire button if using inline onclick previously
    const btn = document.querySelector('.solve-button') || document.querySelector('button.solve-button') || null;
    if (btn) btn.addEventListener('click', sendSolveRequestFromFields);
});
