export async function openPdfStream(fileName, contentStreamReference) {
    try {
        const arrayBuffer = await contentStreamReference.arrayBuffer();
        const blob = new Blob([arrayBuffer], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        
        console.log("JS: Blob created, opening window...");
        
        const newWindow = window.open(url, '_blank');
        
        if (!newWindow) {
            alert("Popup blocked! Please allow popups for localhost.");
        }
    } catch (err) {
        console.error("JS Error in openPdfStream:", err);
    }
}