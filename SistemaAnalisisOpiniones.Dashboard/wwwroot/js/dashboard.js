// Dashboard de indicadores: consume la API del proyecto y dibuja las gráficas con Chart.js.
const colores = { Positiva: "#2e9e6b", Neutra: "#9aa5b1", Negativa: "#d9534f", primario: "#1f3b57", acento: "#e8a33d" };
const graficas = {};

async function getJson(url) {
    const respuesta = await fetch(url);
    if (!respuesta.ok) throw new Error(`${url}: ${respuesta.status}`);
    return respuesta.json();
}

function dibujar(id, config) {
    if (graficas[id]) graficas[id].destroy();
    graficas[id] = new Chart(document.getElementById(id), config);
}

function etiquetaMes(fila) {
    return `${fila.nombreMes.substring(0, 3)} ${fila.anio}`;
}

async function cargarResumen() {
    const r = await getJson("/api/resumen");
    document.getElementById("kpi-total").textContent = r.totalOpiniones.toLocaleString("es");
    document.getElementById("kpi-promedio").textContent = Number(r.promedioSatisfaccion).toFixed(2);
    document.getElementById("kpi-positivas").textContent = `${Number(r.porcentajePositivas).toFixed(1)} %`;
    document.getElementById("kpi-negativas").textContent = `${Number(r.porcentajeNegativas).toFixed(1)} %`;
    document.getElementById("kpi-productos").textContent = r.productosConOpiniones;
    document.getElementById("kpi-clientes").textContent = r.clientesQueOpinan;
    document.getElementById("actualizado").textContent = `Consultado ${new Date().toLocaleString("es")}`;
}

async function cargarSentimientos() {
    const filas = await getJson("/api/sentimientos");
    dibujar("chart-sentimientos", {
        type: "doughnut",
        data: {
            labels: filas.map(f => f.clasificacion),
            datasets: [{ data: filas.map(f => f.opiniones), backgroundColor: filas.map(f => colores[f.clasificacion]) }]
        },
        options: {
            maintainAspectRatio: false,
            plugins: {
                legend: { position: "right" },
                tooltip: { callbacks: { label: c => {
                    const total = c.dataset.data.reduce((a, b) => a + b, 0);
                    return ` ${c.label}: ${c.parsed} (${(100 * c.parsed / total).toFixed(1)} %)`;
                } } }
            }
        }
    });
}

async function cargarFuentes() {
    const filas = await getJson("/api/fuentes");
    dibujar("chart-fuentes", {
        type: "bar",
        data: {
            labels: filas.map(f => f.tipoFuente),
            datasets: [["Positivas", "Positiva"], ["Neutras", "Neutra"], ["Negativas", "Negativa"]].map(([serie, clase]) => ({
                label: serie,
                data: filas.map(f => f[serie.toLowerCase()]),
                backgroundColor: colores[clase]
            }))
        },
        options: {
            maintainAspectRatio: false,
            scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true, title: { display: true, text: "Opiniones" } } }
        }
    });
}

async function cargarTendencia() {
    const filas = await getJson("/api/tendencia");
    dibujar("chart-tendencia", {
        type: "line",
        data: {
            labels: filas.map(etiquetaMes),
            datasets: [
                { label: "% positivas", data: filas.map(f => f.porcentajePositivas), borderColor: colores.Positiva, backgroundColor: colores.Positiva, tension: .3, yAxisID: "y" },
                { label: "% negativas", data: filas.map(f => f.porcentajeNegativas), borderColor: colores.Negativa, backgroundColor: colores.Negativa, tension: .3, yAxisID: "y" },
                { label: "Puntaje promedio (1-5)", data: filas.map(f => f.puntajePromedio), borderColor: colores.primario, backgroundColor: colores.primario, borderDash: [6, 4], tension: .3, yAxisID: "y2" },
                { label: "Opiniones", type: "bar", data: filas.map(f => f.opiniones), backgroundColor: "rgba(31,59,87,.12)", yAxisID: "y3" }
            ]
        },
        options: {
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                y: { position: "left", min: 0, max: 100, title: { display: true, text: "% de opiniones" } },
                y2: { position: "right", min: 1, max: 5, grid: { drawOnChartArea: false }, title: { display: true, text: "Puntaje" } },
                y3: { display: false, beginAtZero: true }
            }
        }
    });
}

async function cargarProductos() {
    const filas = await getJson("/api/productos?top=10");
    dibujar("chart-productos", {
        type: "bar",
        data: {
            labels: filas.map(f => `${f.nombre} (#${f.idProductoOrigen})`),
            datasets: [
                { label: "Opiniones", data: filas.map(f => f.opiniones), backgroundColor: "rgba(31,59,87,.75)", yAxisID: "y" },
                { label: "% satisfacción", data: filas.map(f => f.porcentajeSatisfaccion), type: "line", borderColor: colores.acento, backgroundColor: colores.acento, yAxisID: "y2", tension: .3 }
            ]
        },
        options: {
            maintainAspectRatio: false,
            interaction: { mode: "index", intersect: false },
            scales: {
                y: { beginAtZero: true, title: { display: true, text: "Opiniones" } },
                y2: { position: "right", min: 0, max: 100, grid: { drawOnChartArea: false }, title: { display: true, text: "% satisfacción" } }
            }
        }
    });
}

async function cargarListaProductos() {
    const filas = await getJson("/api/productos/lista");
    const select = document.getElementById("producto");
    select.innerHTML = filas.map(f => `<option value="${f.idProductoOrigen}">${f.nombre} (#${f.idProductoOrigen}) · ${f.opiniones} opiniones</option>`).join("");
}

async function consultarProducto(evento) {
    if (evento) evento.preventDefault();
    const id = document.getElementById("producto").value;
    const desde = document.getElementById("desde").value;
    const hasta = document.getElementById("hasta").value;
    const query = `desde=${desde}&hasta=${hasta}`;
    const [tendencia, opiniones] = await Promise.all([
        getJson(`/api/productos/${id}/tendencia?${query}`),
        getJson(`/api/productos/${id}/opiniones?${query}`)
    ]);

    dibujar("chart-producto", {
        type: "line",
        data: {
            labels: tendencia.map(etiquetaMes),
            datasets: [
                { label: "% positivas", data: tendencia.map(f => f.porcentajePositivas), borderColor: colores.Positiva, backgroundColor: colores.Positiva, tension: .3 },
                { label: "Opiniones", type: "bar", data: tendencia.map(f => f.opiniones), backgroundColor: "rgba(31,59,87,.2)", yAxisID: "y2" }
            ]
        },
        options: {
            maintainAspectRatio: false,
            plugins: { title: { display: true, text: "Tendencia del producto seleccionado" } },
            scales: { y: { min: 0, max: 100, title: { display: true, text: "% positivas" } }, y2: { position: "right", beginAtZero: true, grid: { drawOnChartArea: false }, title: { display: true, text: "Opiniones" } } }
        }
    });

    const cuerpo = document.querySelector("#tabla-opiniones tbody");
    cuerpo.innerHTML = opiniones.map(o => `<tr>
        <td>${o.fecha.substring(0, 10)}</td>
        <td>${o.tipoFuente}</td>
        <td>${o.cliente ?? "<span class='muted'>Anónimo</span>"}</td>
        <td><span class="etiqueta ${o.clasificacion}">${o.clasificacion}</span></td>
        <td>${o.puntajeSatisfaccion ?? "–"}</td>
        <td class="comentario">${escapar(o.comentario ?? "")}</td>
    </tr>`).join("");
    document.getElementById("sin-datos").hidden = opiniones.length > 0;
}

function escapar(texto) {
    return texto.replace(/[&<>"]/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
}

document.getElementById("filtro").addEventListener("submit", consultarProducto);

(async () => {
    try {
        await Promise.all([cargarResumen(), cargarSentimientos(), cargarFuentes(), cargarTendencia(), cargarProductos(), cargarListaProductos()]);
        await consultarProducto();
    } catch (error) {
        console.error(error);
        document.getElementById("actualizado").textContent = "No se pudo consultar el Data Warehouse. Revise la cadena de conexión y que el ETL haya corrido.";
    }
})();
