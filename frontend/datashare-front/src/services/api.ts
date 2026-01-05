import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5180/api",
});

export default api;
