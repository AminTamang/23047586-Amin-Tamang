window.theme = {
    set: theme => {
        document.documentElement.setAttribute("data-theme", theme);
        localStorage.setItem("theme", theme);
    },
    load: () => {
        const theme = localStorage.getItem("theme") || "light";
        document.documentElement.setAttribute("data-theme", theme);
        return theme;
    }
};
