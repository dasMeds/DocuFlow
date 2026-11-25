/* Browser workbench interactions and SignalR updates. */
const uploadForm = document.querySelector("#upload-form");
const fileInput = document.querySelector("#document-files");
const statusFilter = document.querySelector("#status-filter");
const jobsBody = document.querySelector("#jobs-body");
const emptyState = document.querySelector("#empty-state");
const announcement = document.querySelector("#announcement");
const reviewDialog = document.querySelector("#review-dialog");
const reviewForm = document.querySelector("#review-form");
let jobs = [];

/* Keep the current view in sync with the API's canonical job state. */
async function loadJobs() {
  try {
    const response = await fetch("/api/jobs");
    if (!response.ok) throw new Error(`Loading jobs failed (${response.status}).`);
    jobs = await response.json();
    renderJobs();
  } catch (error) {
    announce(error.message);
  }
}

/* Render a filtered job list and update the status cards. */
function renderJobs() {
  const visibleJobs = statusFilter.value === "all"
    ? jobs
    : jobs.filter((job) => job.status === statusFilter.value);
  jobsBody.replaceChildren(...visibleJobs.map(createJobRow));
  emptyState.hidden = visibleJobs.length > 0;
  document.querySelector("#count-all").textContent = jobs.length;
  document.querySelector("#count-processing").textContent =
    jobs.filter((job) => job.status === "Processing" || job.status === "Queued").length;
  document.querySelector("#count-review").textContent =
    jobs.filter((job) => job.status === "NeedsReview").length;
  document.querySelector("#count-completed").textContent =
    jobs.filter((job) => job.status === "Completed").length;
}

/* Construct table cells with text nodes to keep filenames and extracted data inert. */
function createJobRow(job) {
  const row = document.createElement("tr");
  const documentCell = document.createElement("td");
  const name = document.createElement("span");
  const type = document.createElement("span");
  name.className = "document-name";
  name.textContent = job.fileName;
  type.className = "document-type";
  type.textContent = job.fileType;
  documentCell.append(name, type);

  const statusCell = document.createElement("td");
  const badge = document.createElement("span");
  badge.className = `status-badge status-${job.status.toLowerCase()}`;
  badge.textContent = job.status === "NeedsReview" ? "Needs review" : job.status;
  statusCell.append(badge);

  const invoice = job.extractedData ?? {};
  const invoiceCell = textCell(invoice.invoiceNumber || "—");
  const vendorCell = textCell(invoice.vendorName || "—");
  const totalCell = textCell(invoice.totalAmount > 0 ? formatCurrency(invoice.totalAmount) : "—");
  const actionsCell = document.createElement("td");
  actionsCell.className = "row-actions";
  if (job.status === "NeedsReview") {
    actionsCell.append(actionButton("Review", "review", job.id, "button-primary"));
  } else if (job.status === "Failed") {
    actionsCell.append(actionButton("Retry", "retry", job.id, "button-secondary"));
  } else {
    actionsCell.textContent = "—";
  }

  row.append(documentCell, statusCell, invoiceCell, vendorCell, totalCell, actionsCell);
  return row;
}

/* Build a data-only table cell. */
function textCell(value) {
  const cell = document.createElement("td");
  cell.textContent = value;
  return cell;
}

/* Create action controls with their associated job id. */
function actionButton(label, action, id, styleClass) {
  const button = document.createElement("button");
  button.type = "button";
  button.className = `button ${styleClass}`;
  button.dataset.action = action;
  button.dataset.jobId = id;
  button.textContent = label;
  return button;
}

/* Present currency consistently without changing stored decimal values. */
function formatCurrency(value) {
  return new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" }).format(value);
}

/* Surface server and network outcomes in the live announcement area. */
function announce(message) {
  announcement.textContent = message;
}

uploadForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  if (!fileInput.files.length) {
    announce("Choose at least one document first.");
    return;
  }
  try {
    const formData = new FormData();
    for (const file of fileInput.files) formData.append("files", file);
    const response = await fetch("/api/documents", { method: "POST", body: formData });
    if (!response.ok) {
      announce(await response.text());
      return;
    }
    fileInput.value = "";
    announce("Documents accepted and queued.");
    await loadJobs();
  } catch (error) {
    announce(error.message);
  }
});

statusFilter.addEventListener("change", renderJobs);

jobsBody.addEventListener("click", async (event) => {
  const button = event.target.closest("button[data-action]");
  if (!button) return;
  const job = jobs.find((item) => item.id === button.dataset.jobId);
  if (!job) return;
  if (button.dataset.action === "review") {
    openReview(job);
    return;
  }
  try {
    const response = await fetch(`/api/jobs/${encodeURIComponent(job.id)}/retry`, { method: "POST" });
    announce(response.ok ? "Job queued for retry." : await response.text());
    await loadJobs();
  } catch (error) {
    announce(error.message);
  }
});

/* Prefill the review dialog with extracted values before the user approves them. */
function openReview(job) {
  const invoice = job.extractedData ?? {};
  document.querySelector("#review-job-id").value = job.id;
  document.querySelector("#review-invoice-number").value = invoice.invoiceNumber ?? "";
  document.querySelector("#review-vendor-name").value = invoice.vendorName ?? "";
  document.querySelector("#review-issue-date").value = invoice.issueDate?.slice(0, 10) ?? "";
  document.querySelector("#review-subtotal").value = invoice.subtotal ?? 0;
  document.querySelector("#review-tax").value = invoice.taxAmount ?? 0;
  document.querySelector("#review-total").value = invoice.totalAmount ?? 0;
  reviewDialog.showModal();
}

reviewForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const id = document.querySelector("#review-job-id").value;
  const invoice = {
    invoiceNumber: document.querySelector("#review-invoice-number").value.trim(),
    vendorName: document.querySelector("#review-vendor-name").value.trim(),
    issueDate: document.querySelector("#review-issue-date").value || null,
    subtotal: Number(document.querySelector("#review-subtotal").value),
    taxAmount: Number(document.querySelector("#review-tax").value),
    totalAmount: Number(document.querySelector("#review-total").value)
  };
  try {
    const response = await fetch(`/api/jobs/${encodeURIComponent(id)}/approve`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(invoice)
    });
    if (!response.ok) {
      announce(await response.text());
      return;
    }
    reviewDialog.close();
    announce("Invoice approved.");
    await loadJobs();
  } catch (error) {
    announce(error.message);
  }
});

document.querySelector("#close-review").addEventListener("click", () => reviewDialog.close());
document.querySelector("#cancel-review").addEventListener("click", () => reviewDialog.close());

/* SignalR is preferred; periodic refresh remains as a resilient fallback. */
if (window.signalR) {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/pipeline")
    .withAutomaticReconnect()
    .build();
  connection.on("JobUpdated", loadJobs);
  connection.start().catch(() => announce("Live updates unavailable; refreshing periodically."));
}
window.setInterval(loadJobs, 10000);
loadJobs();
